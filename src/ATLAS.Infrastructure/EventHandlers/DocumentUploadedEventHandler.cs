using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class DocumentUploadedEventHandler : INotificationHandler<DocumentUploadedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public DocumentUploadedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.UploadedById,
                "DocumentUploaded",
                "Document",
                notification.DocumentId,
                $"Document {notification.FileName} uploaded to application {notification.ApplicationId} by user {notification.UploadedById}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
