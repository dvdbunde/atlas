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
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                throw new DomainException("Cannot audit document upload: no authenticated user is available.");

            var auditLog = new ATLAS.Domain.Entities.AuditLog(
                _currentUserService.UserId,
                "DocumentUploaded",
                "Document",
                notification.DocumentId,
                $"Document {notification.FileName} uploaded to application {notification.ApplicationId} by user {_currentUserService.UserId}",
                "127.0.0.1"
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
