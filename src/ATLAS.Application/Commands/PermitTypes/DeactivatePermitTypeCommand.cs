using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class DeactivatePermitTypeCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid DeactivatedByAdminId { get; set; }
    }

       public class DeactivatePermitTypeCommandHandler : IRequestHandler<DeactivatePermitTypeCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;
    
        public DeactivatePermitTypeCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
    
        public async Task<bool> Handle(DeactivatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;
    
            permitType.Deactivate(request.DeactivatedByAdminId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeDeactivatedEvent(permitType.Id, request.DeactivatedByAdminId), cancellationToken);
            return true;
        }
    }
}
