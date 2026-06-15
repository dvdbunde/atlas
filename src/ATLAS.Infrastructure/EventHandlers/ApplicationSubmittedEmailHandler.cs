//----------------------
// Application Submitted Email Handler
// Sends confirmation email when application is submitted
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
    public class ApplicationSubmittedEmailHandler : INotificationHandler<ApplicationSubmittedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly ILogger<ApplicationSubmittedEmailHandler> _logger;

        public ApplicationSubmittedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            ILogger<ApplicationSubmittedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationSubmittedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // TODO: Get citizen email from repository
                var citizenEmail = "citizen@example.com"; // Placeholder
                
                var model = new ApplicationSummaryDto
                {
                    ApplicationNumber = notification.ApplicationId.ToString(),
                    PermitTypeName = "Permit Type",
                    Status = ApplicationStatus.Submitted // Submitted
                };

                var body = await _templateRenderer.RenderAsync("SubmissionConfirmation", model, cancellationToken);
                
                await _emailService.SendAsync(
                    citizenEmail,
                    "Application Submitted Successfully",
                    body,
                    isHtml: false,
                    cancellationToken);
                    
                _logger.LogInformation("Sent submission confirmation email for application {ApplicationId}", notification.ApplicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send submission confirmation email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}
