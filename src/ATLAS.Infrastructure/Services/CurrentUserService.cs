using System.Security.Claims;
using ATLAS.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of ICurrentUserService that reads the current user's
    /// identity from the ASP.NET Core HTTP context via IHttpContextAccessor.
    /// 
    /// Design decisions:
    /// - Gracefully handles anonymous requests (returns null/empty defaults, no exceptions)
    /// - Reads UserId from the standard ClaimTypes.NameIdentifier (sub) claim
    /// - Reads Role from ClaimTypes.Role claims — supports multiple roles; returns the first
    /// - All properties are live reads from HttpContext.User — safe because this service is
    ///   registered as Scoped (HttpContext.User is stable within a request)
    /// - No direct dependency on controllers or middleware — purely reads HttpContext
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// The authenticated user's unique identifier, derived from the sub/nameidentifier claim.
        /// Returns null for anonymous requests or if the claim is not present.
        /// </summary>
        public Guid? UserId
        {
            get
            {
                var subClaim = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        /// <summary>
        /// The authenticated user's email address, derived from the email claim.
        /// Returns null for anonymous requests or if the claim is not present.
        /// </summary>
        public string? Email =>
            HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// The authenticated user's application role (Citizen, Officer, Admin).
        /// Returns the first role claim value, or null if none present.
        /// Admins may have multiple role claims; the first one is returned as the primary role.
        /// </summary>
        public string? Role =>
            HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        /// <summary>
        /// Whether the current request has an authenticated user.
        /// </summary>
        public bool IsAuthenticated =>
            HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// The full set of claims for the current user.
        /// Returns an empty read-only collection for anonymous requests.
        /// Live read from HttpContext.User — consistent with all other properties.
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

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
    }
}