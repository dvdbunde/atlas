using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.Applications
{
    public class ApproveApplicationCommand : ICommand<bool>
    {
        public Guid ApplicationId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public ApproveApplicationCommandHandler(
            IApplicationRepository repository,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to approve an application.");

            var officerId = _currentUserService.UserId.Value;
            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.Approve(officerId, request.Comments);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationApprovedEvent(application.Id, officerId), cancellationToken);
            
            return true;
        }
    }
}
