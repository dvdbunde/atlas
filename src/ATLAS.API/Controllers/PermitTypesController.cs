using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries;
using ATLAS.Application.Commands;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PermitTypesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PermitTypesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// Get all permit types (F-17, F-18)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermitTypeDto>>> GetPermitTypes(
            CancellationToken cancellationToken,
            [FromQuery] bool includeInactive = false)
        {
            var query = new GetPermitTypesQuery { IncludeInactive = includeInactive };
            var results = await _mediator.Send(query, cancellationToken);
            return Ok(results);
        }

        /// <summary>
        /// Get permit type by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PermitTypeDto?>> GetPermitTypeById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Create permit type (F-17)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<ActionResult<Guid>> CreatePermitType(
            [FromBody] CreatePermitTypeCommand command,
            CancellationToken cancellationToken)
        {
            var permitTypeId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetPermitTypeById), new { id = permitTypeId }, permitTypeId);
        }

        /// <summary>
        /// Update permit type (F-18)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<bool>> UpdatePermitType(
            [FromRoute] Guid id,
            [FromBody] UpdatePermitTypeCommand command,
            CancellationToken cancellationToken)
        {
            command.PermitTypeId = id;
            var result = await _mediator.Send(command, cancellationToken);
            return result ? Ok(result) : NotFound();
        }

        /// <summary>
        /// Deactivate permit type (F-19)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeactivatePermitType(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = id };
            var result = await _mediator.Send(command, cancellationToken);
            return result ? Ok(result) : NotFound();
        }
    }
}
