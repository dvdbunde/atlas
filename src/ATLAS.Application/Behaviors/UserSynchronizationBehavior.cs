using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;
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
    /// - Calls SaveChangesAsync to persist synchronization changes before the
    ///   handler executes, so the Domain User is always current within the request.
    /// - If synchronization fails, the exception propagates and the handler
    ///   is NOT executed — failing early prevents inconsistent state.
    /// </summary>
    public class UserSynchronizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityResolver _identityResolver;
        private readonly IUnitOfWork _unitOfWork;

        public UserSynchronizationBehavior(
            ICurrentUserService currentUserService,
            IIdentityResolver identityResolver,
            IUnitOfWork unitOfWork)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Proceed to the next behavior or the handler
            return await next();
        }
    }
}
