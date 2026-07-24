using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.PermitTypes
{    
    public class UpdatePermitTypeCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Fee { get; set; }
        public bool? IsActive { get; set; }
    }

        public class UpdatePermitTypeCommandHandler : IRequestHandler<UpdatePermitTypeCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;
    
        public UpdatePermitTypeCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
    
        public async Task<bool> Handle(UpdatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;
    
            if (request.Fee.HasValue)
            {
                var oldFee = permitType.Fee;
                permitType.UpdateFee(request.Fee.Value);
                if (oldFee != request.Fee.Value)
                    await _mediator.Publish(new PermitTypeFeeUpdatedEvent(permitType.Id, oldFee, request.Fee.Value), cancellationToken);
            }
    
            // Activation/Deactivation is handled by the dedicated Activate/Deactivate PermitTypeCommand
    
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
