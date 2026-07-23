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
    public class ApplicationResubmittedEventHandler : INotificationHandler<ApplicationResubmittedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public ApplicationResubmittedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(ApplicationResubmittedEvent notification, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                throw new DomainException("Cannot audit application resubmission: no authenticated user is available.");

            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "ApplicationResubmitted",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} resubmitted by citizen {_currentUserService.UserId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
