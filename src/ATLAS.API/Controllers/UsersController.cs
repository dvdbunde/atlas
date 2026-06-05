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
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// Get all users (F-21)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
            [FromQuery] string? role,
            CancellationToken cancellationToken)
        {
            var query = new GetUsersQuery { Role = role };
            var results = await _mediator.Send(query, cancellationToken);
            return Ok(results);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto?>> GetUserById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Create user account (F-21)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]  // Registration is open
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<ActionResult<Guid>> CreateUser(
            [FromBody] CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            var userId = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUserById), new { id = userId }, userId);
        }

        /// <summary>
        /// Update user role (F-21)
        /// </summary>
        [HttpPut("{id}/role")]
        public async Task<ActionResult<bool>> UpdateUserRole(
            [FromRoute] Guid id,
            [FromBody] UpdateUserRoleCommand command,
            CancellationToken cancellationToken)
        {
            command.UserId = id;
            var result = await _mediator.Send(command, cancellationToken);
            return result ? Ok(result) : NotFound();
        }
    }
}
