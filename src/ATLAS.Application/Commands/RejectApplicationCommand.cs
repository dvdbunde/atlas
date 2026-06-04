using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class RejectApplicationCommand : IRequest<bool>
    {
        public Guid ApplicationId { get; set; }
        public Guid OfficerId { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public RejectApplicationCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.Reject(request.OfficerId, request.ReasonCode, request.Comments);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationRejectedEvent(application.Id, request.OfficerId), cancellationToken);
            
            return true;
        }
    }

    public class ApplicationRejectedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        
        public ApplicationRejectedEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
        }
    }
}
