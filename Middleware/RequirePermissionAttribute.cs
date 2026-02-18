using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VSSAuthPrototype.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

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
            if (!string.IsNullOrWhiteSpace(role) &&
                role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return; // admin bypass
            }

            var permissionClaims = user.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();

            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var val in permissionClaims)
            {
                foreach (var p in val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    perms.Add(p);
            }

            if (!perms.Contains(_permission))
            {
                context.Result = new JsonResult(new { error = $"Forbidden: Missing permission '{_permission}'" })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
