using System.Security.Claims;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of <see cref="IIdentityResolver"/> that resolves
    /// and synchronizes the authenticated user's identity with the Domain <see cref="User"/>
    /// aggregate using claims from <see cref="ICurrentUserService"/> and persistence
    /// via <see cref="IUserRepository"/>.
    ///
    /// Resolution strategy (find-or-create):
    /// 1. Look up Domain User by UserId (sub claim / ClaimTypes.NameIdentifier).
    /// 2. Fallback: look up by Email (ClaimTypes.Email).
    /// 3. If neither found, create a new Domain User from claim values.
    ///
    /// Synchronization (Entra-first model):
    /// - Email, FirstName, LastName, Role ALL derived from Entra ID claims
    /// - Uses <see cref="User.SynchronizeFromClaims"/> (idempotent, no domain events)
    /// - LastLoginDate via user.RecordLogin()
    ///
    /// Design decisions:
    /// - SynchronizeUserAsync internally persists changes and retries on
    ///   concurrent registration races (DbUpdateException). Callers MUST NOT
    ///   call SaveChangesAsync after invoking SynchronizeUserAsync.
    /// - ResolveCurrentUserAsync does NOT persist — it is a pure find-or-create
    ///   operation in memory.
    /// - Uses IUnitOfWork internally for persistence, keeping the retry logic
    ///   self-contained.
    /// - Role synchronization is passive: ATLAS reads the role from Entra claims,
    ///   never writes roles to Entra. Roles are reflected locally for authorization.
    /// </summary>
    public class IdentityResolver : IIdentityResolver
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        // Tracks whether the most recent ResolveCurrentUserAsync call created a new user.
        // Used by SynchronizeUserAsync to decide AddAsync vs UpdateAsync.
        // Reset to false at the start of every ResolveCurrentUserAsync call.
        private bool _isNewlyCreated;

        public IdentityResolver(
            ICurrentUserService currentUserService,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <inheritdoc />
        public async Task<User> ResolveCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new InvalidOperationException("Cannot resolve identity for unauthenticated request.");

            // Reset flag — this call will determine if a new user is needed
            _isNewlyCreated = false;

            var userId = _currentUserService.UserId;
            var email = _currentUserService.Email;

            // Strategy 1: Look up by UserId (sub claim)
            if (userId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                if (user != null)
                    return user;
            }

            // Strategy 2: Look up by Email (fallback for identities without a matching sub claim).
            // Normalize to lower-invariant to match Domain User storage (see User constructor).
            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _userRepository.GetByEmailAsync(email.ToLowerInvariant(), cancellationToken);
                if (user != null)
                    return user;
            }

            // Strategy 3: Create new Domain User from identity claims
            _isNewlyCreated = true;
            return CreateUserFromClaims();
        }

        /// <inheritdoc />
        public async Task<User> SynchronizeUserAsync(CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new InvalidOperationException("Cannot synchronize identity for unauthenticated request.");

            var user = await ResolveCurrentUserAsync(cancellationToken);

            // Synchronize profile properties from claims (idempotent - only updates changed values)
            // This is the Entra-first sync: claims are the single source of truth
            var email = _currentUserService.Email ?? string.Empty;
            var claims = _currentUserService.Claims;
            var firstName = ExtractFirstName(claims) ?? string.Empty;
            var lastName = ExtractLastName(claims) ?? string.Empty;
            var roleClaim = _currentUserService.Role;
            var role = UserRole.Citizen;
            if (!string.IsNullOrWhiteSpace(roleClaim) &&
                Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var parsedRole))
            {
                role = parsedRole;
            }

            user.SynchronizeFromClaims(email, firstName, lastName, role);

            // Record login timestamp
            user.RecordLogin();

            // Persist and retry on concurrent registration race (unique constraint violation).
            // The first attempt may collide with another request creating the same user.
            // On retry, ResolveCurrentUserAsync will find the user created by the first request.
            const int maxAttempts = 2;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (_isNewlyCreated)
                    {
                        await _userRepository.AddAsync(user, cancellationToken);
                    }
                    else
                    {
                        await _userRepository.UpdateAsync(user, cancellationToken);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    break;
                }
                catch (DbUpdateException) when (attempt < maxAttempts)
                {
                    // The concurrent request committed first. Re-resolve to find
                    // the existing user; _isNewlyCreated becomes false so the
                    // retry will call UpdateAsync instead of AddAsync.
                    _isNewlyCreated = false;
                    user = await ResolveCurrentUserAsync(cancellationToken);
                }
            }

            return user;
        }

        private User CreateUserFromClaims()
        {
            var email = _currentUserService.Email ?? "unknown@atlas.local";
            var claims = _currentUserService.Claims;
            var firstName = ExtractFirstName(claims) ?? "Unknown";
            var lastName = ExtractLastName(claims) ?? "User";

            // Parse role from claims, defaulting to Citizen
            var roleClaim = _currentUserService.Role;
            var role = UserRole.Citizen;
            if (!string.IsNullOrWhiteSpace(roleClaim) &&
                Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var parsedRole))
            {
                role = parsedRole;
            }

            // Use oid claim as User ID (matches Entra ID user GUID)
            // Fallback to new GUID if oid is not available
            var userId = _currentUserService.UserId ?? Guid.NewGuid();
            var user = new User(userId, email, firstName, lastName, role);
            user.RecordLogin();
            return user;
        }

        private static string? ExtractFirstName(IReadOnlyCollection<Claim> claims)
        {
            var givenName = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.GivenName)?.Value;

            if (!string.IsNullOrWhiteSpace(givenName))
                return givenName;

            // Fallback: parse from ClaimTypes.Name (e.g., "John Doe" -> "John")
            var name = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name)?.Value;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    return parts[0];
            }

            return null;
        }

        private static string? ExtractLastName(IReadOnlyCollection<Claim> claims)
        {
            var surname = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Surname)?.Value;

            if (!string.IsNullOrWhiteSpace(surname))
                return surname;

            // Fallback: parse from ClaimTypes.Name
            var name = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name)?.Value;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                    return string.Join(" ", parts.Skip(1));
            }

            return null;
        }
    }
}
