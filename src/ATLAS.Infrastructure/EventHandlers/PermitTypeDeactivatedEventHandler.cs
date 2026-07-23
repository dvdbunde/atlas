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
    public class PermitTypeDeactivatedEventHandler : INotificationHandler<PermitTypeDeactivatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public PermitTypeDeactivatedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(PermitTypeDeactivatedEvent notification, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                throw new DomainException("Cannot audit permit type deactivation: no authenticated user is available.");

            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "PermitTypeDeactivated",
                "PermitType",
                notification.PermitTypeId,
                $"Permit type {notification.PermitTypeId} deactivated by admin {_currentUserService.UserId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
