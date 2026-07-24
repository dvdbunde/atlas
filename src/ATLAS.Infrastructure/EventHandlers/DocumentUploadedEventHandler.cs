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
    public class DocumentUploadedEventHandler : INotificationHandler<DocumentUploadedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public DocumentUploadedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "document upload");
                        var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "DocumentUploaded",
                "Document",
                notification.DocumentId,
                $"Document \"{notification.FileName}\" ({notification.DocumentId}) was uploaded to application ({notification.ApplicationId}) by user {AuditGuard.FormatUser(_currentUserService, userId)}.",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
