using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class DocumentDownloadedEventHandler : INotificationHandler<DocumentDownloadedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public DocumentDownloadedEventHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        }

        public async Task Handle(DocumentDownloadedEvent notification, CancellationToken cancellationToken)
        {
            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                notification.DownloadedById,
                "DocumentDownloaded",
                "Document",
                notification.DocumentId,
                $"Document downloaded from application {notification.ApplicationId} by user {notification.DownloadedById}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}