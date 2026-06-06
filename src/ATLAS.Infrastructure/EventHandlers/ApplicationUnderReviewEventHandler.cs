using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationUnderReviewEventHandler : INotificationHandler<ApplicationUnderReviewEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public ApplicationUnderReviewEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(ApplicationUnderReviewEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.OfficerId,
                "ApplicationUnderReview",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} moved to under review by officer {notification.OfficerId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
