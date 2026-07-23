using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeFeeUpdatedEventHandler : INotificationHandler<PermitTypeFeeUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public PermitTypeFeeUpdatedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(PermitTypeFeeUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "PermitTypeFeeUpdated",
                "PermitType",
                notification.PermitTypeId,
                $"Permit type {notification.PermitTypeId} fee changed from {notification.OldFee} to {notification.NewFee}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}