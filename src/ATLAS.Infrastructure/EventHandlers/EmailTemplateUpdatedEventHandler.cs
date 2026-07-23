//----------------------
// Email Template Updated — Audit Handler (Infrastructure)
// Consumes EmailTemplateUpdatedEvent and writes an immutable AuditLog entry using
// the existing repository. This is the ONLY place audit creation for template
// edits lives; the store and renderer remain unaware of auditing.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Domain.Email;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class EmailTemplateUpdatedEventHandler : INotificationHandler<EmailTemplateUpdatedEvent>
    {
        private readonly IAuditLogRepository _auditLogs;
        private readonly ICurrentUserService _currentUserService;

        public EmailTemplateUpdatedEventHandler(
            IAuditLogRepository auditLogs,
            ICurrentUserService currentUserService)
        {
            _auditLogs = auditLogs ?? throw new ArgumentNullException(nameof(auditLogs));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(EmailTemplateUpdatedEvent notification, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                throw new DomainException("Cannot audit email template update: no authenticated user is available.");

            var auditLog = new AuditLog(
                _currentUserService.UserId,
                "Updated",
                "EmailTemplate",
                notification.EntityId,
                $"Email template '{notification.TemplateName}' updated.",
                string.Empty);

            await _auditLogs.AddAsync(auditLog, cancellationToken);
        }
    }
}

