using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class UpdatePermitTypeGeneralInformationCommand : ICommand<bool>
    {
        public Guid PermitTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdatePermitTypeGeneralInformationCommandHandler : IRequestHandler<UpdatePermitTypeGeneralInformationCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;

        public UpdatePermitTypeGeneralInformationCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(UpdatePermitTypeGeneralInformationCommand request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);

            if (permitType == null)
                return false;

            permitType.UpdateGeneralInformation(request.Name, request.Description);

            await _repository.UpdateAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeGeneralInformationUpdatedEvent(permitType.Id, request.Name, request.Description), cancellationToken);
            return true;
        }
    }
}
