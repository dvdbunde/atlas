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
    public class ApplicationAssignedToOfficerEventHandler : INotificationHandler<ApplicationAssignedToOfficerEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public ApplicationAssignedToOfficerEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(ApplicationAssignedToOfficerEvent notification, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                throw new DomainException("Cannot audit application assignment: no authenticated user is available.");

            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "ApplicationAssignedToOfficer",
                "Application",
                notification.ApplicationId,
                $"Application {notification.ApplicationId} assigned to officer {_currentUserService.UserId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
