using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.Applications
{
    [Obsolete("Use CreateDraftCommand + SubmitDraftCommand instead")]
    public class SubmitApplicationCommand : ICommand<Guid>
    {
        public Guid PermitTypeId { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
    }

    public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, Guid>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public SubmitApplicationCommandHandler(
            IApplicationRepository repository,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<Guid> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to submit an application.");

            var citizenId = _currentUserService.UserId.Value;
            var application = new Entities.Application(citizenId, request.PermitTypeId, request.CitizenNotes);
            application.Submit();
            
            await _repository.AddAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationSubmittedEvent(application.Id, citizenId, request.PermitTypeId), cancellationToken);
            
            return application.Id;
        }
    }
}
