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

        public UpdatePermitTypeCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(UpdatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            
            if (permitType == null)
                return false;
            
            if (request.Fee.HasValue)
                permitType.UpdateFee(request.Fee.Value);
            
            if (request.IsActive.HasValue && request.IsActive.Value)
                permitType.Activate();
            
            // Deactivation is handled by the dedicated DeactivatePermitTypeCommand
            
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
