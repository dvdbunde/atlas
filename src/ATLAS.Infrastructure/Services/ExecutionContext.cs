using System.Security.Claims;
using ATLAS.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Infrastructure implementation of <see cref="IExecutionContext"/> that wraps
    /// <see cref="ICurrentUserService"/> for user identity and generates a
    /// <see cref="CorrelationId"/> once per HTTP request.
    ///
    /// Design decisions:
    /// - Registered as Scoped — CorrelationId is generated once per request
    ///   and reused for all operations within that scope.
    /// - Delegates to ICurrentUserService for identity rather than reading
    ///   HttpContext directly (Clean Architecture rule enforcement).
    /// - CorrelationId is generated eagerly in the constructor (not lazily),
    ///   ensuring it is stable across all consumers within the same scope.
    /// - IP address resolution is delegated to a future enhancement
    ///   (requires IHttpContextAccessor for HttpContext.Connection.RemoteIpAddress).
    /// </summary>
    public class ExecutionContext : IExecutionContext
    {
        private readonly ICurrentUserService _currentUserService;

        public ExecutionContext(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

            // Generate CorrelationId once per scope (HTTP request)
            CorrelationId = Guid.NewGuid();
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
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public bool IsAuthenticated => _currentUserService.IsAuthenticated;
    }
}