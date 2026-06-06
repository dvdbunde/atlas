using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationSubmittedEventHandler : INotificationHandler<ApplicationSubmittedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationSubmittedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationSubmittedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.CitizenId,
                "ApplicationSubmitted",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} submitted by citizen {notification.CitizenId} for permit type {notification.PermitTypeId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
