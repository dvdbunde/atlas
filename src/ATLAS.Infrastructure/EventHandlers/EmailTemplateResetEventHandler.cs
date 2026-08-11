//----------------------
// Email Template Reset — Audit Handler (Infrastructure)
// Consumes EmailTemplateResetEvent and writes an immutable AuditLog entry using
// the existing repository. Mirrors EmailTemplateUpdatedEventHandler.
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
    public class EmailTemplateResetEventHandler : INotificationHandler<EmailTemplateResetEvent>
    {
        private readonly IAuditLogRepository _auditLogs;
        private readonly ICurrentUserService _currentUserService;

        public EmailTemplateResetEventHandler(
            IAuditLogRepository auditLogs,
            ICurrentUserService currentUserService)
        {
            _auditLogs = auditLogs ?? throw new ArgumentNullException(nameof(auditLogs));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(EmailTemplateResetEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "email template reset");
            var auditLog = new AuditLog(
                userId,
                "Reset",
                "EmailTemplate",
                notification.EntityId,
                $"Email template \"{notification.TemplateName}\" was reset to its default.",
                string.Empty);

            await _auditLogs.AddAsync(auditLog, cancellationToken);
        }
    }
}
