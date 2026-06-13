using System.Security.Claims;
using ATLAS.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of ICurrentUserService that reads the current user's
    /// identity from the ASP.NET Core HTTP context via IHttpContextAccessor.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// The authenticated user's unique identifier, derived from the oid claim (Entra ID v2.0)
        /// or sub/nameidentifier claim (v1.0 fallback).
        /// Returns null for anonymous requests or if neither claim is present or valid.
        /// </summary>
        public Guid? UserId
        {
            get
            {
                // Try oid claim first (Entra ID v2.0) - check both short and long form
                var oidClaim = FindClaim("oid") ?? FindClaim("http://schemas.microsoft.com/identity/claims/objectidentifier");
                if (oidClaim != null && Guid.TryParse(oidClaim.Value, out var oidGuid))
                {
                    return oidGuid;
                }

                // Fallback to sub claim (v1.0 tokens or other identity providers)
                var subClaim = FindClaim(ClaimTypes.NameIdentifier);
                if (subClaim != null && Guid.TryParse(subClaim.Value, out var subGuid))
                {
                    return subGuid;
                }

                return null;
            }
        }

        /// <summary>
        /// The authenticated user's email address, derived from the email claim.
        /// Returns null for anonymous requests or if the claim is not present.
        /// </summary>
        public string? Email
        {
            get
            {
                // Try standard email claims first
                var email = FindClaim(ClaimTypes.Email)?.Value ?? 
                        FindClaim("email")?.Value ?? 
                        FindClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                
                if (!string.IsNullOrWhiteSpace(email))
                    return email;
                
                // Fallback: try preferred_username (often contains email for Entra ID)
                var preferredUsername = FindClaim("preferred_username")?.Value;
                if (!string.IsNullOrWhiteSpace(preferredUsername) && preferredUsername.Contains("@"))
                    return preferredUsername;
                
                // Fallback: try UPN
                var upn = FindClaim("upn")?.Value;
                if (!string.IsNullOrWhiteSpace(upn) && upn.Contains("@"))
                    return upn;
                
                return null;
            }
        }

        /// <summary>
        /// The authenticated user's application role (Citizen, Officer, Admin).
        /// Returns the first role claim value, or null if none present.
        /// </summary>
        public string? Role =>
            FindClaim(ClaimTypes.Role)?.Value ?? 
            FindClaim("roles")?.Value;

        /// <summary>
        /// Whether the current request has an authenticated user.
        /// </summary>
        public bool IsAuthenticated =>
            HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// The full set of claims for the current user.
        /// Returns an empty read-only collection for anonymous requests.
        /// </summary>
        public IReadOnlyCollection<Claim> Claims
        {
            get
            {
                var user = HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    return Array.Empty<Claim>();
                }
                return user.Claims.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Helper to find a claim by type, checking both short and full URI forms.
        /// </summary>
        private Claim? FindClaim(string claimType)
        {
            var user = HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            // Try exact match first
            var claim = user.FindFirst(claimType);
            if (claim != null)
                return claim;

            // Try case-insensitive match (some providers use different casing)
            return user.Claims.FirstOrDefault(c => 
                string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
    }
}