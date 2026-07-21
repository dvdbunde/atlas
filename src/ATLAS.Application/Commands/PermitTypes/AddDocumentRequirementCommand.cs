using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
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

        public AddDocumentRequirementCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
            return true;
        }
    }
}
