using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VSSAuthPrototype.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireSubscriptionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredPlan;

        public RequireSubscriptionAttribute(string requiredPlan = "basic")
        {
            _requiredPlan = requiredPlan;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new JsonResult(new 
                { 
                    error = "Unauthorized",
                    message = "You must be logged in to access this content",
                    requiresAuth = true
                })
                {
                    StatusCode = 401
                };
                return;
            }

            // Get user's role and subscription from token
            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var subscriptionPlan = user.Claims.FirstOrDefault(c => c.Type == "subscriptionPlan")?.Value;

            // Admins always bypass subscription checks
            if (role?.ToLower() == "admin")
            {
                return;
            }

            // Check if subscription meets requirement
            if (!HasRequiredSubscription(subscriptionPlan, _requiredPlan))
            {
                context.Result = new JsonResult(new 
                { 
                    error = "Payment Required",
                    message = $"This content requires a {_requiredPlan} subscription or higher",
                    currentPlan = subscriptionPlan ?? "none",
                    requiredPlan = _requiredPlan,
                    upgradeUrl = "/subscribe"
                })
                {
                    StatusCode = 402 // Payment Required
                };
            }
        }

        private bool HasRequiredSubscription(string? userPlan, string requiredPlan)
        {
            if (string.IsNullOrEmpty(userPlan)) return false;

            // Subscription hierarchy: premium > express > basic
            var planHierarchy = new Dictionary<string, int>
            {
                { "basic", 1 },
                { "express", 2 },
                { "giveaway", 2 },
                { "premium", 3 }
            };

            var userLevel = planHierarchy.GetValueOrDefault(userPlan.ToLower(), 0);
            var requiredLevel = planHierarchy.GetValueOrDefault(requiredPlan.ToLower(), 0);

            return userLevel >= requiredLevel;
        }
    }
}
