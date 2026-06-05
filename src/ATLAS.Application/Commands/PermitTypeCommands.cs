using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands
{
    public class CreatePermitTypeCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; } = 0;
    }

    public class CreatePermitTypeCommandHandler : IRequestHandler<CreatePermitTypeCommand, Guid>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;

        public CreatePermitTypeCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Guid> Handle(CreatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = new PermitType(request.Name, request.Description, request.Fee);
            
            await _repository.AddAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeActivatedEvent(permitType.Id), cancellationToken);
            
            return permitType.Id;
        }
    }

    public class UpdatePermitTypeCommand : IRequest<bool>
    {
        public Guid PermitTypeId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? EstimatedProcessingDays { get; set; }
        public bool? IsActive { get; set; }
        public Guid DeactivatedByAdminId { get; set; }
    }

    public class UpdatePermitTypeCommandHandler : IRequestHandler<UpdatePermitTypeCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public UpdatePermitTypeCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(UpdatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            
            if (permitType == null)
                return false;
            
            // Note: Name and Description are read-only (set in constructor)
            // For MVP, we skip updating these fields
            // TODO: Add UpdateDetails method to PermitType entity if needed
            
            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    permitType.Activate();
                else
                    permitType.Deactivate(request.DeactivatedByAdminId);
            }
            
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }

    public class DeactivatePermitTypeCommand : IRequest<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid DeactivatedByAdminId { get; set; }
    }

    public class DeactivatePermitTypeCommandHandler : IRequestHandler<DeactivatePermitTypeCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public DeactivatePermitTypeCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(DeactivatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            
            if (permitType == null)
                return false;
            
            permitType.Deactivate(request.DeactivatedByAdminId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
