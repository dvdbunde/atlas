using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationAssignedToOfficerEventHandler : INotificationHandler<ApplicationAssignedToOfficerEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationAssignedToOfficerEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationAssignedToOfficerEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.OfficerId,
                "ApplicationAssignedToOfficer",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} assigned to officer {notification.OfficerId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
