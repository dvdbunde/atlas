using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class MovePermitFieldCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public Guid FieldId { get; set; }
        public int NewOrder { get; set; }
    }

    public class MovePermitFieldCommandHandler : IRequestHandler<MovePermitFieldCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;

        public MovePermitFieldCommandHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<bool> Handle(MovePermitFieldCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            permitType.MoveField(request.FieldId, request.NewOrder);
            await _repository.UpdateAsync(permitType, cancellationToken);
            return true;
        }
    }
}
