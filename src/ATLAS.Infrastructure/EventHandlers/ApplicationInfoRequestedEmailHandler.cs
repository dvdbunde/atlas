//----------------------
// Application Info Requested Email Handler
// Sends info request email when officer requests additional information
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationInfoRequestedEmailHandler : INotificationHandler<ApplicationInfoRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly ILogger<ApplicationInfoRequestedEmailHandler> _logger;

        public ApplicationInfoRequestedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            ILogger<ApplicationInfoRequestedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationInfoRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // TODO: Get citizen email from repository
                var citizenEmail = "citizen@example.com"; // Placeholder
                
                var model = new ApplicationSummaryDto
                {
                    ApplicationNumber = notification.ApplicationId.ToString(),
                    PermitTypeName = "Permit Type",
                    Status = 5 // Info Requested
                };

                var body = await _templateRenderer.RenderAsync("InfoRequestNotification", model, cancellationToken);
                
                await _emailService.SendAsync(
                    citizenEmail,
                    "Additional Information Requested",
                    body,
                    isHtml: false,
                    cancellationToken);
                    
                _logger.LogInformation("Sent info request email for application {ApplicationId}", notification.ApplicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send info request email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}
