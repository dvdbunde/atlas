using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class RemoveDocumentRequirementCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid RequirementId { get; set; }
    }

    public class RemoveDocumentRequirementCommandHandler : IRequestHandler<RemoveDocumentRequirementCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public RemoveDocumentRequirementCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(RemoveDocumentRequirementCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.RemoveDocumentRequirement(request.RequirementId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
