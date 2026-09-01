using System.Security.Claims;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Provides request-scoped execution context including the authenticated user's
    /// identity.
    ///
    /// Design decisions:
    /// - User identity properties delegate to <see cref="ICurrentUserService"/>
    ///   rather than reading HttpContext directly (Clean Architecture rule enforcement).
    /// - Technical correlation is handled exclusively by W3C trace context
    ///   (System.Diagnostics.Activity.Current); a separate application-level
    ///   correlation ID was removed in O3 as redundant (it had no consumers).
    /// </summary>
    public interface IExecutionContext
    {
        /// <summary>
        /// The unique identifier of the current authenticated user, or null for anonymous requests.
        /// Delegates to <see cref="ICurrentUserService.UserId"/>.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// The email of the current authenticated user, or null for anonymous requests.
        /// Delegates to <see cref="ICurrentUserService.Email"/>.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// The application role of the current user (e.g., "Citizen", "Officer", "Admin"),
        /// or null for anonymous requests.
        /// Delegates to <see cref="ICurrentUserService.Role"/>.
        /// </summary>
        string? Role { get; }

        /// <summary>
        /// The full set of claims for the current user.
        /// Delegates to <see cref="ICurrentUserService.Claims"/>.
        /// </summary>
        IReadOnlyCollection<Claim> Claims { get; }

        /// <summary>
        /// Whether the current request has an authenticated user.
        /// Delegates to <see cref="ICurrentUserService.IsAuthenticated"/>.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}