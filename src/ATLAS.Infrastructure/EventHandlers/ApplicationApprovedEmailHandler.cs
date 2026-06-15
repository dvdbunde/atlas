//----------------------
// Application Approved Email Handler
// Sends approval email when application is approved
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using ATLAS.Domain.Enums;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationApprovedEmailHandler : INotificationHandler<ApplicationApprovedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly ILogger<ApplicationApprovedEmailHandler> _logger;

        public ApplicationApprovedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            ILogger<ApplicationApprovedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationApprovedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // TODO: Get citizen email from repository
                var citizenEmail = "citizen@example.com"; // Placeholder
                
                var model = new ApplicationSummaryDto
                {
                    ApplicationNumber = notification.ApplicationId.ToString(),
                    PermitTypeName = "Permit Type",
                    Status = ApplicationStatus.Approved // Approved
                };

                var body = await _templateRenderer.RenderAsync("ApprovalNotification", model, cancellationToken);
                
                await _emailService.SendAsync(
                    citizenEmail,
                    "Application Approved",
                    body,
                    isHtml: false,
                    cancellationToken);
                    
                _logger.LogInformation("Sent approval email for application {ApplicationId}", notification.ApplicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}
