using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VSSAuthPrototype.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdminAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new JsonResult(new { error = "Unauthorized: Authentication required" })
                {
                    StatusCode = 401
                };
                return;
            }

            var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (role?.ToLower() != "admin")
            {
                context.Result = new JsonResult(new { error = "Forbidden: Admin access required" })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
