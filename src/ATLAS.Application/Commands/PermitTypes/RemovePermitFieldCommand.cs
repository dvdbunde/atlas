using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
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
        private readonly IMediator _mediator;

        public RemovePermitFieldCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(RemovePermitFieldCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            if (permitType == null)
                return false;

            var field = permitType.Fields.FirstOrDefault(f => f.Id == request.FieldId);
            var fieldName = field?.Name ?? request.FieldId.ToString();

            permitType.RemoveField(request.FieldId);
            await _repository.UpdateAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeFieldRemovedEvent(permitType.Id, request.FieldId, fieldName), cancellationToken);
            return true;
        }
    }
}
