using System.Security.Claims;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Provides request-scoped execution context including the authenticated user's
    /// identity and a correlation ID for tracing a single user action across
    /// multiple domain events and audit log entries.
    ///
    /// Design decisions:
    /// - <see cref="CorrelationId"/> is a new Guid generated once per HTTP request,
    ///   enabling end-to-end tracing of a user action that triggers multiple
    ///   domain events (e.g., application approval triggers ApplicationApproved
    ///   + AuditLog entry).
    /// - User identity properties delegate to <see cref="ICurrentUserService"/>
    ///   rather than reading HttpContext directly (Clean Architecture rule enforcement).
    /// - <see cref="IpAddress"/> is resolved from the HTTP context for audit trail
    ///   completeness (currently hardcoded as "127.0.0.1" in event handlers).
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
        /// A stable identifier for the current HTTP request, generated once and
        /// reused across all operations within the same request scope.
        /// Enables correlating audit log entries, domain events, and log messages
        /// that originate from a single user action.
        /// </summary>
        Guid CorrelationId { get; }

        /// <summary>
        /// Whether the current request has an authenticated user.
        /// Delegates to <see cref="ICurrentUserService.IsAuthenticated"/>.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}