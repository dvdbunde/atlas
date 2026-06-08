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
    public partial class UsersController : ControllerBase, IUsersController
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<ICollection<UserResponse>> UsersGetAsync(string? role = null)
        {
            var query = new GetUsersQuery { Role = role };
            var results = await _mediator.Send(query, default);
            var response = new List<UserResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return response;
        }

        public async Task<Guid> UsersPostAsync(CreateUserRequest body)
        {
            var command = new CreateUserCommand
            {
                Email = body.Email,
                FirstName = body.FirstName,
                LastName = body.LastName,
                Role = body.Role,
                Department = body.Department
            };
            return await _mediator.Send(command, default);
        }

        public async Task<UserResponse> UsersGetAsync(Guid id)
        {
            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, default);
            return result?.ToResponse();
        }

        public async Task<bool> RoleAsync(Guid id, UpdateUserRoleRequest body)
        {
            var command = new UpdateUserRoleCommand
            {
                UserId = id,
                Role = body.Role
            };
            return await _mediator.Send(command, default);
        }
    }
}
