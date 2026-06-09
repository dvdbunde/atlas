//----------------------
// Users Controller Adapter
// Implements IUsersController using MediatR
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
    public partial class UsersController : ControllerBase, IUsersController
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _implementation = this;
        }

        public async Task<ActionResult<ICollection<UserResponse>>> UsersGetAsync(string? role = null)
        {
            var query = new GetUsersQuery { Role = role };
            var results = await _mediator.Send(query, default);
            var response = new List<UserResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return Ok(response);
        }

        public async Task<ActionResult<Guid>> UsersPostAsync(CreateUserRequest body)
        {
            var command = new CreateUserCommand
            {
                Email = body.Email,
                FirstName = body.FirstName,
                LastName = body.LastName,
                Role = body.Role,
                Department = body.Department
            };
            var userId = await _mediator.Send(command, default);
            return CreatedAtAction(nameof(UsersGetAsync), new { id = userId }, userId);
        }

        public async Task<ActionResult<UserResponse>> UsersGetAsync(Guid id)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, default);
            if (result == null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public async Task<ActionResult<bool>> RoleAsync(Guid id, UpdateUserRoleRequest body)
        {
            var command = new UpdateUserRoleCommand
            {
                UserId = id,
                Role = body.Role
            };
            var result = await _mediator.Send(command, default);
            return Ok(result);
        }
    }
}