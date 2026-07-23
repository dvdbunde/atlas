using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class UpdatePermitFieldCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid FieldId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FieldType Type { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }

    public class UpdatePermitFieldCommandHandler : IRequestHandler<UpdatePermitFieldCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public UpdatePermitFieldCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(UpdatePermitFieldCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.UpdateField(
                request.FieldId,
                request.Name,
                request.Type,
                request.IsRequired,
                request.DefaultValue,
                request.Options);

            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
