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
