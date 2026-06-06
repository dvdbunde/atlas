using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationInfoRequestedEventHandler : INotificationHandler<ApplicationInfoRequestedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationInfoRequestedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationInfoRequestedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.OfficerId,
                "ApplicationInfoRequested",
                "Application",
                notification.ApplicationId,
                $"Info requested for application {notification.ApplicationId} by officer {notification.OfficerId}: {notification.Message}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
