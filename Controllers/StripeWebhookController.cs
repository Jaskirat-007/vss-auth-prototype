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

        /// <summary>
        /// POST /api/stripe/webhook
        /// Stripe sends checkout.session.completed here after a successful payment.
        /// We read the price ID, map it to a subscription tier, and update Clerk metadata.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            Event stripeEvent;

            try
            {
                // If webhook secret is configured, verify the signature
                if (!string.IsNullOrEmpty(webhookSecret))
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        webhookSecret
                    );
                }
                else
                {
                    // For local testing without CLI, parse without verification
                    stripeEvent = EventUtility.ParseEvent(json);
                }
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe webhook signature verification failed: {e.Message}");
                return BadRequest(new { error = "Invalid signature" });
            }

            // Handle the checkout.session.completed event
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session == null)
                    return BadRequest(new { error = "Invalid session data" });

                Console.WriteLine($"Checkout completed for customer: {session.CustomerEmail}");

                // Get the line items to find which price/product was purchased
                var service = new SessionService();
                StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

                var lineItems = await service.ListLineItemsAsync(session.Id);
                var priceId = lineItems.Data.FirstOrDefault()?.Price?.Id;

                if (string.IsNullOrEmpty(priceId))
                {
                    Console.WriteLine("No price ID found in checkout session");
                    return Ok();
                }

                // Map price ID to subscription tier
                var tier = MapPriceToTier(priceId);
                Console.WriteLine($"Price {priceId} mapped to tier: {tier}");

                // Get Clerk user ID from session metadata or customer email
                var clerkUserId = session.Metadata?.GetValueOrDefault("clerk_user_id");
                var customerEmail = session.CustomerEmail ?? session.CustomerDetails?.Email;

                if (!string.IsNullOrEmpty(clerkUserId))
                {
                    // Update Clerk user metadata with new subscription plan
                    await UpdateClerkUserMetadata(clerkUserId, tier);
                    Console.WriteLine($"Updated Clerk user {clerkUserId} to {tier}");
                }
                else if (!string.IsNullOrEmpty(customerEmail))
                {
                    // Find user by email and update
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

        /// <summary>
        /// POST /api/stripe/create-portal-session
        /// Creates a Stripe Customer Portal session so users can manage their subscription.
        /// Frontend calls this from openBillingPortal().
        /// </summary>
        [HttpPost("create-portal-session")]
        public async Task<IActionResult> CreatePortalSession()
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            // Get the user's email from their Clerk JWT claims
            var email = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "No email found in auth token" });
            }

            // Find the Stripe customer by email
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

            // Create a portal session
            var portalService = new Stripe.BillingPortal.SessionService();
            var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customer.Id,
                ReturnUrl = _configuration["Stripe:PortalReturnUrl"] ?? "http://localhost:3000/dashboard"
            });

            return Ok(new { url = portalSession.Url });
        }

        /// <summary>
        /// Maps a Stripe price ID to a VSS subscription tier.
        /// Add more mappings here if you add tiers later.
        /// </summary>
        private string MapPriceToTier(string priceId)
        {
            var premiumPriceId = _configuration["Stripe:PremiumPriceId"];

            if (priceId == premiumPriceId)
                return "premium";

            // Default fallback — any paid product = premium for now
            return "premium";
        }

        /// <summary>
        /// Updates a Clerk user's public metadata with the new subscription plan.
        /// Uses the Clerk Backend API.
        /// </summary>
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

        /// <summary>
        /// Finds a Clerk user by email address.
        /// Returns the Clerk user ID if found, null otherwise.
        /// </summary>
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