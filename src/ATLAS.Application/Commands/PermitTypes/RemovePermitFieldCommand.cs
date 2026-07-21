using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class RemovePermitFieldCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid FieldId { get; set; }
    }

    public class RemovePermitFieldCommandHandler : IRequestHandler<RemovePermitFieldCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public RemovePermitFieldCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(RemovePermitFieldCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.RemoveField(request.FieldId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
