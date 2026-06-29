using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Events;

namespace ATLAS.Application.Commands.PermitTypes
{
    public class CreatePermitTypeCommand : ICommand<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; } = 0;
    }

    public class CreatePermitTypeCommandHandler : IRequestHandler<CreatePermitTypeCommand, Guid>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;

        public CreatePermitTypeCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Guid> Handle(CreatePermitTypeCommand request, CancellationToken cancellationToken)
        {
            var permitType = new PermitType(request.Name, request.Description, request.Fee);
            
            await _repository.AddAsync(permitType, cancellationToken);
            await _mediator.Publish(new PermitTypeActivatedEvent(permitType.Id), cancellationToken);
            
            return permitType.Id;
        }
    }    
}
