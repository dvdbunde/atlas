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
    public class ApplicationApprovedEventHandler : INotificationHandler<ApplicationApprovedEvent>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ICurrentUserService _currentUserService;

        public ApplicationApprovedEventHandler(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task Handle(ApplicationApprovedEvent notification, CancellationToken cancellationToken)
        {
            var userId = AuditGuard.RequireAuthenticatedUser(_currentUserService, "application approval");
                        var auditLog = new ATLAS.Domain.Entities.AuditLog(
                userId,
                "ApplicationApproved",
                "Application",
                notification.ApplicationId,
                $"Application ({notification.ApplicationId}) was approved by administrator {AuditGuard.FormatUser(_currentUserService, userId)}.",
                "127.0.0.1" // TODO: Get actual IP address from HttpContext
            );

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}
