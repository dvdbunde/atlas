using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeFieldUpdatedEventHandler : INotificationHandler<PermitTypeFieldUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public PermitTypeFieldUpdatedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(PermitTypeFieldUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "Updated",
                "PermitField",
                notification.FieldId,
                $"Field '{notification.FieldName}' updated in permit type {notification.PermitTypeId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}