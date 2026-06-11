using ATLAS.Application.Interfaces;
using MediatR;

namespace ATLAS.Application.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that synchronizes the authenticated user's
    /// identity with the Domain User aggregate before every request handler.
    ///
    /// This ensures that every authenticated request has a corresponding Domain
    /// User record, with up-to-date profile properties and login timestamps.
    ///
    /// Pipeline position: runs BEFORE the request handler, after ValidationBehavior.
    ///
    /// Design decisions:
    /// - Skips synchronization for unauthenticated requests (anonymous endpoints
    ///   like user registration).
    /// - Delegates resolution and synchronization to <see cref="IIdentityResolver"/>,
    ///   keeping this behavior thin and testable.
    /// - SynchronizeUserAsync internally persists and retries on concurrent
    ///   registration races (see IdentityResolver for retry logic).
    /// - If synchronization fails, the exception propagates and the handler
    ///   is NOT executed — failing early prevents inconsistent state.
    /// </summary>
    public class UserSynchronizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityResolver _identityResolver;

        public UserSynchronizationBehavior(
            ICurrentUserService currentUserService,
            IIdentityResolver identityResolver)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Skip synchronization for unauthenticated requests
            if (_currentUserService.IsAuthenticated)
            {
                await _identityResolver.SynchronizeUserAsync(cancellationToken);
            }

            // NOTE: SynchronizeUserAsync commits its changes in a separate
            // transaction from the handler's IUnitOfWork.SaveChangesAsync().
            // This means the sync save and the handler save are two separate
            // transaction boundaries. If the handler fails after sync succeeds,
            // the user sync changes remain committed.
            //
            // This is an accepted design trade-off for Option A: keeping the
            // retry-on-conflict logic self-contained inside IdentityResolver.
            // A future improvement could wrap both sync + handler in an
            // IDbContextTransaction, but that would require either:
            //   (a) moving the transaction orchestration to Infrastructure, or
            //   (b) accepting the Application layer dependency on EF Core.

            // Proceed to the next behavior or the handler
            return await next();
        }
    }
}
