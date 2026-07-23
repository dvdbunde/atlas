using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
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
        private readonly IMediator _mediator;

        public RemoveDocumentRequirementCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(RemoveDocumentRequirementCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            var requirement = permitType.DocumentRequirements.FirstOrDefault(d => d.Id == request.RequirementId);
            var documentType = requirement?.DocumentType ?? request.RequirementId.ToString();

            permitType.RemoveDocumentRequirement(request.RequirementId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeDocumentRequirementRemovedEvent(permitType.Id, request.RequirementId, documentType), cancellationToken);
            return true;
        }
    }
}
