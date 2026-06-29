using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands.Applications
{
    public class AssignToOfficerCommand : ICommand<bool>
    {
        public Guid ApplicationId { get; set; }
        public Guid OfficerId { get; set; }
    }

    public class AssignToOfficerCommandHandler : IRequestHandler<AssignToOfficerCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public AssignToOfficerCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(AssignToOfficerCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            application.AssignToOfficer(request.OfficerId);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationAssignedToOfficerEvent(application.Id, request.OfficerId), cancellationToken);
            
            return true;
        }
    }
}
