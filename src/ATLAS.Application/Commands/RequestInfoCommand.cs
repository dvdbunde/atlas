using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class RequestInfoCommand : IRequest<bool>
    {
        public Guid ApplicationId { get; set; }
        public Guid OfficerId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RequestInfoCommandHandler : IRequestHandler<RequestInfoCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public RequestInfoCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(RequestInfoCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.RequestInfo(request.OfficerId, request.Message);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationInfoRequestedEvent(application.Id, request.OfficerId), cancellationToken);
            
            return true;
        }
    }

    public class ApplicationInfoRequestedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        
        public ApplicationInfoRequestedEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
        }
    }
}
