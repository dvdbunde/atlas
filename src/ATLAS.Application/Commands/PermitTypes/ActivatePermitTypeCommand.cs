using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class ActivatePermitTypeCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid ActivatedByAdminId { get; set; }
    }

    public class ActivatePermitTypeCommandHandler : IRequestHandler<ActivatePermitTypeCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public ActivatePermitTypeCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(ActivatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.Activate();
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}