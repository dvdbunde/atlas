using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeActivatedEventHandler : INotificationHandler<PermitTypeActivatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public PermitTypeActivatedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(PermitTypeActivatedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.ActivatedByAdminId,
                "PermitTypeActivated",
                "PermitType",
                notification.PermitTypeId,
                $"Permit type {notification.PermitTypeId} activated by admin {notification.ActivatedByAdminId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
