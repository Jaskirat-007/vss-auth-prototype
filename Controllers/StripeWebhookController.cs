//stripe

using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VSSAuthPrototype.Controllers
{
    [ApiController]
    [Route("api/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public StripeWebhookController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            Event stripeEvent;

            try
            {
                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        webhookSecret,
                        throwOnApiVersionMismatch: false
                    );
                }
                else
                {
                    stripeEvent = EventUtility.ParseEvent(json, throwOnApiVersionMismatch: false);
                }
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe webhook signature verification failed: {e.Message}");
                return BadRequest(new { error = "Invalid signature" });
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session == null)
                    return BadRequest(new { error = "Invalid session data" });

                Console.WriteLine($"Checkout completed for customer: {session.CustomerEmail}");

                var service = new SessionService();
                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

                var lineItems = await service.ListLineItemsAsync(session.Id);
                var priceId = lineItems.Data.FirstOrDefault()?.Price?.Id;

                if (string.IsNullOrEmpty(priceId))
                {
                    Console.WriteLine("No price ID found in checkout session");
                    return Ok();
                }

                var tier = MapPriceToTier(priceId);
                Console.WriteLine($"Price {priceId} mapped to tier: {tier}");

                var clerkUserId = session.Metadata?.GetValueOrDefault("clerk_user_id");
                var customerEmail = session.CustomerEmail ?? session.CustomerDetails?.Email;

                if (!string.IsNullOrEmpty(clerkUserId))
                {
                    await UpdateClerkUserMetadata(clerkUserId, tier);
                    Console.WriteLine($"Updated Clerk user {clerkUserId} to {tier}");
                }
                else if (!string.IsNullOrEmpty(customerEmail))
                {
                    var userId = await FindClerkUserByEmail(customerEmail);
                    if (userId != null)
                    {
                        await UpdateClerkUserMetadata(userId, tier);
                        Console.WriteLine($"Updated Clerk user (by email {customerEmail}) to {tier}");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find Clerk user for email: {customerEmail}");
                    }
                }
                else
                {
                    Console.WriteLine("No clerk_user_id or email found in checkout session");
                }
            }

            return Ok();
        }

        [HttpPost("create-portal-session")]
        public async Task<IActionResult> CreatePortalSession()
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            var email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "No email found in auth token" });
            }

            var customerService = new CustomerService();
            var customers = await customerService.ListAsync(new CustomerListOptions
            {
                Email = email,
                Limit = 1
            });

            var customer = customers.Data.FirstOrDefault();

            if (customer == null)
            {
                return BadRequest(new { error = "No Stripe customer found for this email. Subscribe first." });
            }

            var portalService = new Stripe.BillingPortal.SessionService();
            var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customer.Id,
                ReturnUrl = _configuration["Stripe:PortalReturnUrl"] ?? "http://localhost:3000/dashboard"
            });

            return Ok(new { url = portalSession.Url });
        }

        private string MapPriceToTier(string priceId)
        {
            var premiumPriceId = _configuration["Stripe:PremiumPriceId"];

            if (priceId == premiumPriceId)
                return "premium";

            return "premium";
        }

        private async Task UpdateClerkUserMetadata(string clerkUserId, string subscriptionPlan)
        {
            var clerkApiKey = _configuration["Clerk:ApiKey"];
            if (string.IsNullOrEmpty(clerkApiKey))
            {
                Console.WriteLine("Clerk API key not configured");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clerkApiKey);

            var payload = new
            {
                public_metadata = new
                {
                    subscriptionPlan = subscriptionPlan,
                    subscriptionUpdatedAt = DateTime.UtcNow.ToString("o")
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PatchAsync(
                $"https://api.clerk.com/v1/users/{clerkUserId}",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to update Clerk metadata: {response.StatusCode} - {error}");
            }
        }

        private async Task<string?> FindClerkUserByEmail(string email)
        {
            var clerkApiKey = _configuration["Clerk:ApiKey"];
            if (string.IsNullOrEmpty(clerkApiKey)) return null;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clerkApiKey);

            var response = await client.GetAsync(
                $"https://api.clerk.com/v1/users?email_address={Uri.EscapeDataString(email)}"
            );

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<JsonElement>(json);

            if (users.ValueKind == JsonValueKind.Array && users.GetArrayLength() > 0)
            {
                return users[0].GetProperty("id").GetString();
            }

            return null;
        }
    }
}