using System.Security.Claims;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

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
    /// Synchronization updates:
    /// - Email if changed
    /// - FirstName from ClaimTypes.GivenName (or parsed from ClaimTypes.Name)
    /// - LastName from ClaimTypes.Surname (or parsed from ClaimTypes.Name)
    /// - LastLoginDate via user.RecordLogin()
    ///
    /// Design decision: does NOT call SaveChangesAsync — the caller (e.g., pipeline
    /// behavior) owns the persist operation, keeping this composable with IUnitOfWork.
    /// </summary>
    public class IdentityResolver : IIdentityResolver
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;

        public IdentityResolver(
            ICurrentUserService currentUserService,
            IUserRepository userRepository)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <inheritdoc />
        public async Task<User> ResolveCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new InvalidOperationException("Cannot resolve identity for unauthenticated request.");

            var userId = _currentUserService.UserId;
            var email = _currentUserService.Email;

            // Strategy 1: Look up by UserId (sub claim)
            if (userId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
                if (user != null)
                    return user;
            }

            // Strategy 2: Look up by Email (fallback for identities without a matching sub claim)
            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
                if (user != null)
                    return user;
            }

            // Strategy 3: Create new Domain User from identity claims
            return CreateUserFromClaims();
        }

        /// <inheritdoc />
        public async Task<User> SynchronizeUserAsync(CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsAuthenticated)
                throw new InvalidOperationException("Cannot synchronize identity for unauthenticated request.");

            var user = await ResolveCurrentUserAsync(cancellationToken);

            // Synchronize profile properties from claims
            var email = _currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(email) &&
                !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.UpdateEmail(email);
            }

            // Extract name parts from claims
            var claims = _currentUserService.Claims;
            var firstName = ExtractFirstName(claims);
            var lastName = ExtractLastName(claims);

            if (firstName != null || lastName != null)
            {
                var currentFirstName = firstName ?? user.FirstName;
                var currentLastName = lastName ?? user.LastName;

                if (!string.Equals(user.FirstName, currentFirstName, StringComparison.Ordinal) ||
                    !string.Equals(user.LastName, currentLastName, StringComparison.Ordinal))
                {
                    user.UpdateProfile(currentFirstName, currentLastName);
                }
            }

            // Record login timestamp
            user.RecordLogin();

            // Persist changes via repository (caller owns SaveChangesAsync)
            await _userRepository.UpdateAsync(user, cancellationToken);

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

            var user = new User(email, firstName, lastName, role);
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
