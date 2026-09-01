using System.Security.Claims;
using ATLAS.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of <see cref="IExecutionContext"/> that wraps
    /// <see cref="ICurrentUserService"/> for user identity.
    ///
    /// Design decisions:
    /// - Registered as Scoped — identity is resolved once per request/scope.
    /// - Delegates to ICurrentUserService for identity rather than reading
    ///   HttpContext directly (Clean Architecture rule enforcement).
    /// - CorrelationId was removed in O3: it was generated but never consumed.
    ///   W3C Activity trace context is the single technical correlation mechanism.
    /// </summary>
    public class ExecutionContext : IExecutionContext
    {
        private readonly ICurrentUserService _currentUserService;

        public ExecutionContext(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <inheritdoc />
        public Guid? UserId => _currentUserService.UserId;

        /// <inheritdoc />
        public string? Email => _currentUserService.Email;

        /// <inheritdoc />
        public string? Role => _currentUserService.Role;

        /// <inheritdoc />
        public IReadOnlyCollection<Claim> Claims => _currentUserService.Claims;

        /// <inheritdoc />
        public bool IsAuthenticated => _currentUserService.IsAuthenticated;
    }
}