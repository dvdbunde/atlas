using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeFieldAddedEventHandler : INotificationHandler<PermitTypeFieldAddedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public PermitTypeFieldAddedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(PermitTypeFieldAddedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                Guid.Empty, // System action, no specific user
                "PermitTypeFieldAdded",
                "PermitType",
                notification.PermitTypeId,
                $"Field '{notification.FieldName}' added to permit type {notification.PermitTypeId} (type: {notification.FieldType})",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
