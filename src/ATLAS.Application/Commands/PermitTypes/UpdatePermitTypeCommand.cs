using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands
{    
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
}
