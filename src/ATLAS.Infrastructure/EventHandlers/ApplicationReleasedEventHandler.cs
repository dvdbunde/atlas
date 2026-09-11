using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationReleasedEventHandler : INotificationHandler<ApplicationReleasedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public ApplicationReleasedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(ApplicationReleasedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "application release");
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "ApplicationReleased",
                "Application",
                notification.ApplicationId,
                $"Application ({notification.ApplicationId}) was released by officer {AuditGuard.FormatUser(_currentUserService, userId)} and returned to Submitted (unassigned).",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
