using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.Users
{
    public class GetUsersQuery : IRequest<IEnumerable<UserDto>>
    {
        public string? Role { get; set; }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
    {
        private readonly IUserRepository _repository;

        public GetUsersQueryHandler(IUserRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _repository.GetAllAsync(cancellationToken);
            
            // Filter by role if specified
            if (!string.IsNullOrEmpty(request.Role))
            {
                if (Enum.TryParse<ATLAS.Domain.Entities.UserRole>(request.Role, out var role))
                    users = users.Where(u => u.Role == role).ToList();
            }
            
            // Map to UserDto (read-only, synchronized from Entra)
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString(),
                LastLoginDate = u.LastLoginDate
            }).ToList();
        }
    }   
}
