using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Aplicacion_pedidos.Filters
{
    public class AuthorizeRolesAttribute : TypeFilterAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles) : base(typeof(AuthorizeRolesFilter))
        {
            Arguments = new object[] { roles };
        }
    }

    public class AuthorizeRolesFilter : IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRolesFilter(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                // User is not authenticated, redirect to login
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            // Check if roles are specified and user has one of them
            if (_roles.Length > 0)
            {
                bool authorized = false;
                
                // Get all the user's role claims
                var roleClaims = context.HttpContext.User.FindAll(ClaimTypes.Role);
                
                foreach (var role in _roles)
                {
                    // Check if any of the user's roles match the required roles
                    foreach (var claim in roleClaims)
                    {
                        if (claim.Value.Equals(role, StringComparison.OrdinalIgnoreCase))
                        {
                            authorized = true;
                            break;
                        }
                    }
                    if (authorized) break;
                }

                // If user doesn't have any of the required roles
                if (!authorized)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                }
            }
        }
    }
}