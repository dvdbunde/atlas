using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationResubmittedEventHandler : INotificationHandler<ApplicationResubmittedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationResubmittedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationResubmittedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.CitizenId,
                "ApplicationResubmitted",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} resubmitted by citizen {notification.CitizenId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
