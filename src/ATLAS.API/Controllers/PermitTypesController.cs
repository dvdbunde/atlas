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

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Produces("application/json")]    
    public sealed class PermitTypesController : PermitTypesControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public PermitTypesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));            
        }

        public override async Task<ActionResult<ICollection<PermitTypeResponse>>> PermittypesGet(bool? includeInactive = false)
        {
            var query = new GetPermitTypesQuery { IncludeInactive = includeInactive ?? false };
            var results = await _mediator.Send(query, default);
            var response = new List<PermitTypeResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return Ok(response);
        }

        public override async Task<ActionResult<Guid>> PermittypesPost(CreatePermitTypeRequest body)
        {
            var command = new CreatePermitTypeCommand
            {
                Name = body.Name,
                Description = body.Description,
                Fee = body.Fee
            };
            var permitTypeId = await _mediator.Send(command, default);
            return CreatedAtAction(nameof(PermittypesGet), new { id = permitTypeId }, permitTypeId);
        }

        public override async Task<ActionResult<PermitTypeResponse>> PermittypesGet(Guid id)
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = id };
            var result = await _mediator.Send(query, default);
            if (result == null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public override async Task<ActionResult<bool>> PermittypesPut(Guid id, UpdatePermitTypeRequest body)
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

            if (!result)
            {
                return NotFound(); // ← Permit type not found
            }
            
            return Ok(true);
        }

        public override async Task<ActionResult<bool>> PermittypesDelete(Guid id)
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = id };
            var result = await _mediator.Send(command, default);
            if (!result)
            {
                return NotFound(); // ← Permit type not found
            }
            
            return NoContent(); // ← 204 for successful DELETE
        }       
    }
}