using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class AddDocumentRequirementCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public long MaxFileSizeBytes { get; set; }
    }

    public class AddDocumentRequirementCommandHandler : IRequestHandler<AddDocumentRequirementCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;

        public AddDocumentRequirementCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(AddDocumentRequirementCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.AddDocumentRequirement(
                request.DocumentType,
                request.IsRequired,
                request.AllowedExtensions,
                request.MaxFileSizeBytes);

            await _repository.UpdateAsync(permitType, cancellationToken);
            var requirement = permitType.DocumentRequirements.FirstOrDefault(d => d.DocumentType.Equals(request.DocumentType, StringComparison.OrdinalIgnoreCase));
            if (requirement != null)
                await _mediator.Publish(new PermitTypeDocumentRequirementAddedEvent(permitType.Id, requirement.Id, request.DocumentType), cancellationToken);
            return true;
        }
    }
}
