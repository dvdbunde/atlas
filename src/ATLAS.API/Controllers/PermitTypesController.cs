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
using System.Collections.Generic;
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

        public async Task<ActionResult<ICollection<PermitTypeResponse>>> PermittypesGetAsync(bool includeInactive)
        {
            var query = new GetPermitTypesQuery { IncludeInactive = includeInactive };
            var results = await _mediator.Send(query, default);
            var response = new List<PermitTypeResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return Ok(response);
        }

        public async Task<ActionResult<Guid>> PermittypesPostAsync(CreatePermitTypeRequest body)
        {
            var command = new CreatePermitTypeCommand
            {
                Name = body.Name,
                Description = body.Description,
                Fee = body.Fee
            };
            var permitTypeId = await _mediator.Send(command, default);
            return CreatedAtAction(nameof(PermittypesGetAsync), new { id = permitTypeId }, permitTypeId);
        }

        public async Task<ActionResult<PermitTypeResponse>> PermittypesGetAsync(Guid id)
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = id };
            var result = await _mediator.Send(query, default);
            if (result == null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public async Task<ActionResult<bool>> PermittypesPutAsync(Guid id, UpdatePermitTypeRequest body)
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
            var result = await _mediator.Send(command, default);
            return Ok(result);
        }

        public async Task<ActionResult<bool>> PermittypesDeleteAsync(Guid id)
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = id };
            var result = await _mediator.Send(command, default);
            return Ok(result);
        }
    }
}