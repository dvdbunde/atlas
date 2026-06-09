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

namespace ATLAS.API.Controllers
{
    [ApiController]    
    [Produces("application/json")]
    public sealed class UsersController : UsersControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));            
        }

        public override async Task<ActionResult<ICollection<UserResponse>>> UsersGet(string? role = null)
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

        public override async Task<ActionResult<Guid>> UsersPost(CreateUserRequest body)
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
            return CreatedAtAction(nameof(UsersGet), new { id = userId }, userId);
        }

        public override async Task<ActionResult<UserResponse>> UsersGet(Guid id)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, default);
            if (result == null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public override async Task<ActionResult<bool>> Role(Guid id, UpdateUserRoleRequest body)
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