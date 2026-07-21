using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.Admin
{
    /// <summary>
    /// Query to retrieve a single user's read-only detail projection.
    /// Includes the most recent audit entries for the principal (read-only).
    /// </summary>
    public class GetUserByIdQuery : IRequest<UserDetailDto?>
    {
        public Guid UserId { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository, IAuditLogRepository auditLogRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                return null;

            var auditEntries = (await _auditLogRepository.GetByUserIdAsync(user.Id, cancellationToken))
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    Timestamp = a.Timestamp,
                    IpAddress = a.IpAddress
                })
                .ToList();

            return new UserDetailDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.GetFullName(),
                Email = user.Email,
                Role = user.Role,
                LastLoginDate = user.LastLoginDate,
                CreatedDate = null,
                RecentAuditEntries = auditEntries
            };
        }
    }
}
