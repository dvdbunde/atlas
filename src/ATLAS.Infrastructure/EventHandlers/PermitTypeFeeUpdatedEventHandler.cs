using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeFeeUpdatedEventHandler : INotificationHandler<PermitTypeFeeUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public PermitTypeFeeUpdatedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(PermitTypeFeeUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                null,
                "PermitTypeFeeUpdated",
                "PermitType",
                notification.PermitTypeId,
                $"Permit type {notification.PermitTypeId} fee changed from {notification.OldFee} to {notification.NewFee}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}