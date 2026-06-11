using ATLAS.Domain.Entities;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Resolves the authenticated user's identity and synchronizes claims with
    /// the Domain User aggregate. This is the Application-layer abstraction for
    /// identity resolution, following the Clean Architecture pattern alongside
    /// <see cref="ICurrentUserService"/>.
    ///
    /// Responsibilities:
    /// - Resolve the Domain User for the current authenticated identity (find-or-create).
    /// - Synchronize the Domain User's profile properties from identity claims.
    ///
    /// Design decisions:
    /// - Does NOT persist changes — callers (e.g., pipeline behaviors) own the
    ///   save operation, keeping this composable with <see cref="IUnitOfWork"/>.
    /// - Uses <see cref="ICurrentUserService"/> for claim access rather than
    ///   reading HttpContext directly (Clean Architecture rule enforcement).
    /// - Uses <see cref="IUserRepository"/> for Domain User persistence operations,
    ///   keeping domain logic in the Domain layer.
    /// </summary>
    public interface IIdentityResolver
    {
        /// <summary>
        /// Finds the Domain User associated with the current authenticated identity,
        /// or creates a new Domain User if none exists.
        /// Does NOT call SaveChangesAsync — the caller is responsible for persisting.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resolved (found or newly created) Domain User.</returns>
        Task<User> ResolveCurrentUserAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes the Domain User's profile properties (Email, FirstName,
        /// LastName) and LastLoginDate from the current identity claims.
        /// If no Domain User exists, it will be created via <see cref="ResolveCurrentUserAsync"/>.
        /// Does NOT call SaveChangesAsync — the caller is responsible for persisting.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The synchronized Domain User, with all claim-driven updates applied.</returns>
        Task<User> SynchronizeUserAsync(CancellationToken cancellationToken = default);
    }
}
