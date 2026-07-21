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
    public class AddPermitFieldCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FieldType Type { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public List<string>? Options { get; set; }
    }

    public class AddPermitFieldCommandHandler : IRequestHandler<AddPermitFieldCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public AddPermitFieldCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(AddPermitFieldCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.AddField(
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
