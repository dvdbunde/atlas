using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class UserRoleChangedEventHandler : INotificationHandler<UserRoleChangedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public UserRoleChangedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(UserRoleChangedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.UserId,
                "UserRoleChanged",
                "User",
                notification.UserId,
                $"User {notification.UserId} role changed from {notification.OldRole} to {notification.NewRole}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
