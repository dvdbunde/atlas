using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace ATLAS.API.Auth
{
    /// <summary>
    /// Transforms claims from Entra ID / Identity tokens into application role claims.
    /// 
    /// Mapping rules:
    /// - Entra ID: maps 'roles' claim values (Citizen, Officer, Admin) -> ClaimTypes.Role
    /// - Checks Entra ID 'groups' claim (group ObjectId) if roles not present (future)
    /// - ASP.NET Core Identity: 'role' claim values are already mapped by Identity
    /// - Preserves all existing claims; only adds Role claims if missing
    /// 
    /// Registered as Scoped in DI — runs once per request after JWT validation.
    /// </summary>
    public class AtlasClaimsTransformation : IClaimsTransformation
    {
        private static readonly HashSet<string> ValidRoles = new(
            new[] { "Citizen", "Officer", "Admin" },
            StringComparer.OrdinalIgnoreCase);

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return Task.FromResult(principal ?? new ClaimsPrincipal(new ClaimsIdentity()));

            var identity = (ClaimsIdentity)principal.Identity;

            // Check if the principal already has an application role claim
            var existingRoles = principal.FindAll(ClaimTypes.Role)
                .Where(c => ValidRoles.Contains(c.Value))
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (existingRoles.Count > 0)
                return Task.FromResult(principal);

            // Map Entra ID 'roles' claim values -> ClaimTypes.Role
            var rolesFromToken = principal.FindAll("roles")
                .Where(c => ValidRoles.Contains(c.Value));

            var newIdentity = new ClaimsIdentity(identity.Claims, identity.AuthenticationType);

            foreach (var roleClaim in rolesFromToken)
            {
                newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
            }

            var newPrincipal = new ClaimsPrincipal(newIdentity);
            return Task.FromResult(newPrincipal);
        }
    }
}
