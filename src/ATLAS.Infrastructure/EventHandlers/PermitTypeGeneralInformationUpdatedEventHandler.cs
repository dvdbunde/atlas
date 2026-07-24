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
    public class PermitTypeGeneralInformationUpdatedEventHandler : INotificationHandler<PermitTypeGeneralInformationUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public PermitTypeGeneralInformationUpdatedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(PermitTypeGeneralInformationUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "permit type general information update");

            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "PermitTypeGeneralInformationUpdated",
                "PermitType",
                notification.PermitTypeId,
                $"General information for permit type \"{notification.Name}\" ({notification.PermitTypeId}) was updated by administrator {AuditGuard.FormatUser(_currentUserService, userId)}.",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
