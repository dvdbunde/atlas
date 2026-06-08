using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATLAS.API.Infrastructure
{
    /// <summary>
    /// Applies authorization policies to generated controllers based on their name/interface.
    /// This ensures generated controllers have proper authorization without attributes.
    /// </summary>
    public class GeneratedControllerAuthorizationConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Skip non-generated controllers (they have their own attributes)
            if (!controller.ControllerType.FullName?.Contains("Generated") == true)
            {
                return;
            }

            // Determine the required roles based on controller name
            var roles = GetRolesForController(controller.ControllerName);

            if (!string.IsNullOrEmpty(roles))
            {
                // Add AuthorizeFilter with roles
                var policy = new AuthorizationPolicyBuilder()
                    .RequireRole(roles.Split(','))
                    .Build();
                var authorizeFilter = new AuthorizeFilter(policy);
                controller.Filters.Add(authorizeFilter);
            }
        }

        private static string? GetRolesForController(string controllerName)
        {
            return controllerName.ToLowerInvariant() switch
            {
                // Admin-only controllers
                "permittypes" => "Admin",
                "users" => "Admin",
                "auditlogs" => "Admin",

                // Multi-role controllers (Citizen + Officer + Admin)
                "applications" => "Citizen,Officer,Admin",
                "documents" => "Citizen,Officer,Admin",

                // Default: no authorization (should not happen)
                _ => null
            };
        }
    }
}
