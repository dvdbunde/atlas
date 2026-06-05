using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeDeactivatedEventHandler : INotificationHandler<PermitTypeDeactivatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public PermitTypeDeactivatedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(PermitTypeDeactivatedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.DeactivatedByAdminId,
                "PermitTypeDeactivated",
                "PermitType",
                notification.PermitTypeId,
                $"Permit type {notification.PermitTypeId} deactivated by admin {notification.DeactivatedByAdminId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
