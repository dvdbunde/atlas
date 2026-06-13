using ATLAS.Domain.Entities;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Resolves the authenticated user's identity and synchronizes Entra ID claims with
    /// the Domain User aggregate. This is the Application-layer abstraction for
    /// identity resolution in an Entra-first architecture, following Clean Architecture
    /// alongside <see cref="ICurrentUserService"/>.
    ///
    /// Responsibilities:
    /// - Resolve the Domain User for the current authenticated identity (find-or-create).
    /// - Synchronize the Domain User's profile properties (email, name, role) from
    ///   Entra ID claims via <see cref="User.SynchronizeFromClaims"/>.
    ///
    /// Key behaviors (Entra-first):
    /// - User creation is automatic and idempotent (first request creates, subsequent finds).
    /// - User updates are automatic and idempotent (claims diff'd against stored values).
    /// - Role updates are derived from Entra only — ATLAS never writes roles.
    /// - Profile updates are derived from Entra only — ATLAS never writes profiles.
    ///
    /// Stored identity information:
    /// - External identity identifier (Entra ID oid → User.Id)
    /// - Display information (email, first name, last name)
    /// - Role information (Citizen/Officer/Admin)
    /// - Last seen/information (LastLoginDate)
    ///
    /// Design decisions:
    /// - <see cref="SynchronizeUserAsync"/> internally persists changes and handles
    ///   retry-on-conflict for concurrent registration races (see Infrastructure
    ///   implementation for details). Callers MUST NOT call SaveChangesAsync after
    ///   invoking SynchronizeUserAsync — the persistence is owned by this method.
    /// - <see cref="ResolveCurrentUserAsync"/> does NOT persist — it is a pure
    ///   resolution operation (find-or-create in memory only).
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
        /// Does NOT persist — the caller is responsible for calling SaveChangesAsync
        /// to commit this operation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resolved (found or newly created) Domain User.</returns>
        Task<User> ResolveCurrentUserAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes the Domain User's profile properties (Email, FirstName,
        /// LastName) and LastLoginDate from the current identity claims.
        /// If no Domain User exists, it will be created via <see cref="ResolveCurrentUserAsync"/>.
        /// Internally persists changes and handles retry-on-conflict for concurrent
        /// registration races. Callers MUST NOT call SaveChangesAsync after invoking
        /// this method — persistence is owned internally.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The synchronized Domain User, with all claim-driven updates applied.</returns>
        Task<User> SynchronizeUserAsync(CancellationToken cancellationToken = default);
    }
}
