using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class PermitTypeDocumentRequirementUpdatedEventHandler : INotificationHandler<PermitTypeDocumentRequirementUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public PermitTypeDocumentRequirementUpdatedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(PermitTypeDocumentRequirementUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "document requirement update");
                        var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "Updated",
                "DocumentRequirement",
                notification.DocumentRequirementId,
                $"Document requirement \"{notification.DocumentType}\" ({notification.DocumentRequirementId}) was updated in permit type ({notification.PermitTypeId}) by administrator {AuditGuard.FormatUser(_currentUserService, userId)}. Required: {notification.IsRequired}.",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}