//----------------------
// PermitTypes Controller Adapter
// Implements IPermitTypesController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Commands;
using ATLAS.Application.Queries;
using System.Threading.Tasks;

namespace ATLAS.API.Controllers.Generated
{
    public partial class PermitTypesController : ControllerBase, IPermitTypesController
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public PermitTypesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _implementation = this;
        }

        public async Task<ICollection<PermitTypeResponse>> PermittypesGetAsync(bool includeInactive)
        {
            var query = new GetPermitTypesQuery { IncludeInactive = includeInactive };
            var results = await _mediator.Send(query, default);
            var response = new List<PermitTypeResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return response;
        }

        public async Task<Guid> PermittypesPostAsync(CreatePermitTypeRequest body)
        {
            var command = new CreatePermitTypeCommand
            {
                Name = body.Name,
                Description = body.Description,
                Fee = body.Fee
            };
            return await _mediator.Send(command, default);
        }

        public async Task<PermitTypeResponse> PermittypesGetAsync(Guid id)
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = id };
            var result = await _mediator.Send(query, default);
            return result?.ToResponse();
        }

        public async Task<bool> PermittypesPutAsync(Guid id, UpdatePermitTypeRequest body)
        {
            var command = new UpdatePermitTypeCommand
            {
                PermitTypeId = id,
                Name = body.Name,
                Description = body.Description,                
                IsActive = body.IsActive,
                DeactivatedByAdminId = body.DeactivatedByAdminId ?? Guid.Empty,
                EstimatedProcessingDays = body.EstimatedProcessingDays
            };
            return await _mediator.Send(command, default);
        }

        public async Task<bool> PermittypesDeleteAsync(Guid id)
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = id };
            return await _mediator.Send(command, default);
        }
    }
}
