using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class UpdateDocumentRequirementCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid RequirementId { get; set; }
        public bool IsRequired { get; set; }
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public long MaxFileSizeBytes { get; set; }
    }

    public class UpdateDocumentRequirementCommandHandler : IRequestHandler<UpdateDocumentRequirementCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public UpdateDocumentRequirementCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(UpdateDocumentRequirementCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.UpdateDocumentRequirement(
                request.RequirementId,
                request.IsRequired,
                request.AllowedExtensions,
                request.MaxFileSizeBytes);

            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
