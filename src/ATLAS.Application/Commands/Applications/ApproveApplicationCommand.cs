using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class ApproveApplicationCommand : IRequest<bool>
    {
        public Guid ApplicationId { get; set; }
        public Guid OfficerId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public ApproveApplicationCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(ApproveApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.Approve(request.OfficerId, request.Comments);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationApprovedEvent(application.Id, request.OfficerId), cancellationToken);
            
            return true;
        }
    }

    public class ApplicationApprovedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        
        public ApplicationApprovedEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
        }
    }
}
