using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class SubmitApplicationCommand : IRequest<Guid>
    {
        public Guid CitizenId { get; set; }
        public Guid PermitTypeId { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
    }

    public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, Guid>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public SubmitApplicationCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Guid> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = new Entities.Application(request.CitizenId, request.PermitTypeId, request.CitizenNotes);
            application.Submit();
            
            await _repository.AddAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationSubmittedEvent(application.Id), cancellationToken);
            
            return application.Id;
        }
    }

    public class ApplicationSubmittedEvent : INotification
    {
        public Guid ApplicationId { get; }
        
        public ApplicationSubmittedEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
        }
    }
}
