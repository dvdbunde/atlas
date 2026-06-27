using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class RejectApplicationCommand : ICommand<bool>
    {
        public Guid ApplicationId { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public RejectApplicationCommandHandler(
            IApplicationRepository repository,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to reject an application.");

            var officerId = _currentUserService.UserId.Value;
            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.Reject(officerId, request.ReasonCode, request.Comments);            
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new Domain.Events.ApplicationRejectedEvent(application.Id, officerId, request.ReasonCode), cancellationToken);
            
            return true;
        }
    }
}
