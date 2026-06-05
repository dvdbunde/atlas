using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationRejectedEventHandler : INotificationHandler<ApplicationRejectedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationRejectedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationRejectedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.OfficerId,
                "ApplicationRejected",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} rejected by officer {notification.OfficerId}. Reason: {notification.ReasonCode}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
