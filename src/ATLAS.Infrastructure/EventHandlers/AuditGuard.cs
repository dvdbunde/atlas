using System;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;

namespace ATLAS.Infrastructure.EventHandlers
{
    /// <summary>
    /// Centralizes authentication validation for audit logging. This is the single
    /// supported mechanism for resolving the acting user inside audit handlers. The
    /// AuditLog constructor remains the final defensive safeguard.
    /// </summary>
    public static class AuditGuard
    {
        /// <summary>
        /// Verifies the current user is authenticated and has a resolved UserId.
        /// </summary>
        /// <param name="currentUserService">The current user service.</param>
        /// <param name="action">A short description of the audited action, used in the error message.</param>
        /// <returns>The authenticated user's id.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="currentUserService"/> is null.</exception>
        /// <exception cref="DomainException">Thrown when no authenticated user is available.</exception>
        public static Guid RequireAuthenticatedUser(ICurrentUserService currentUserService, string action)
        {
            if (currentUserService == null)
                throw new ArgumentNullException(nameof(currentUserService));

            if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
                throw new DomainException($"Cannot audit {action}: no authenticated user is available.");

            return currentUserService.UserId.Value;
        }

        /// <summary>
        /// Formats the acting user for an audit message as "Display Name" (Guid) or
        /// (Guid) when no display name is available, keeping the immutable identifier
        /// for traceability.
        /// </summary>
        public static string FormatUser(ICurrentUserService currentUserService, Guid userId)
        {
            var email = currentUserService?.Email;
            return email != null ? $"\"{email}\" ({userId})" : $"({userId})";
        }
    }
}
