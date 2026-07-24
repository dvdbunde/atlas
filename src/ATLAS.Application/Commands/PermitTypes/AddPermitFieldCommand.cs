using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
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
        public string DefaultValue { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }

       public class AddPermitFieldCommandHandler : IRequestHandler<AddPermitFieldCommand, bool>
    {
        private readonly IPermitTypeRepository _repository;
        private readonly IMediator _mediator;
    
        public AddPermitFieldCommandHandler(IPermitTypeRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
            await _mediator.Publish(new PermitTypeFieldAddedEvent(permitType.Id, permitType.Fields.Last().Id, request.Name, request.Type), cancellationToken);
            return true;
        }
    }
}
