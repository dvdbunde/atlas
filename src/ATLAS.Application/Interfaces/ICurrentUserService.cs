using System.Security.Claims;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Provides access to the current authenticated user's identity, roles, and claims
    /// from Entra ID tokens.
    ///
    /// In the Entra-first architecture, this service extracts identity claims that are
    /// synchronized to the Domain User aggregate via IIdentityResolver:
    /// - UserId from the Entra oid claim (external identity identifier)
    /// - Email from Entra email/preferred_username claims
    /// - Role from Entra roles/role claims
    /// - Claims for display information (given_name, family_name, etc.)
    ///
    /// This is the Application-layer abstraction for accessing security context,
    /// keeping Clean Architecture dependency rules intact — no HttpContext dependency
    /// leaks into the inner layers.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// The unique identifier of the current authenticated user, or null for anonymous requests.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// The email address of the current authenticated user, or null for anonymous requests.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// The application role of the current user (e.g., "Citizen", "Officer", "Admin"),
        /// or null for anonymous requests.
        /// </summary>
        string? Role { get; }

        /// <summary>
        /// Whether the current request is from an authenticated user.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// The full set of claims for the current user. Empty for anonymous requests.
        /// </summary>
        IReadOnlyCollection<Claim> Claims { get; }
    }
}