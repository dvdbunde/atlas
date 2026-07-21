using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class MoveDocumentRequirementCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid RequirementId { get; set; }
        public int NewOrder { get; set; }
    }

    public class MoveDocumentRequirementCommandHandler : IRequestHandler<MoveDocumentRequirementCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public MoveDocumentRequirementCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(MoveDocumentRequirementCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.MoveDocumentRequirement(request.RequirementId, request.NewOrder);
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
