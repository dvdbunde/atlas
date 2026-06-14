using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Commands
{
    public class RequestInfoCommand : IRequest<bool>
    {
        public Guid ApplicationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RequestInfoCommandHandler : IRequestHandler<RequestInfoCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public RequestInfoCommandHandler(
            IApplicationRepository repository,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(RequestInfoCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to request info.");

            var officerId = _currentUserService.UserId.Value;
            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.RequestInfo(officerId, request.Message);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new Domain.Events.ApplicationInfoRequestedEvent(application.Id, officerId, request.Message), cancellationToken);
            
            return true;
        }
    }
}