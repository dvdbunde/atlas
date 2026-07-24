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
    public class DocumentDownloadedEventHandler : INotificationHandler<DocumentDownloadedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public DocumentDownloadedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(DocumentDownloadedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "document download");
                        var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "DocumentDownloaded",
                "Document",
                notification.DocumentId,
                $"Document ({notification.DocumentId}) was downloaded from application ({notification.ApplicationId}) by user {AuditGuard.FormatUser(_currentUserService, userId)}.",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}