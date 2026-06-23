//----------------------
// Application Info Requested Email Handler
// Sends info request email when officer requests additional information
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationInfoRequestedEmailHandler : INotificationHandler<ApplicationInfoRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ILogger<ApplicationInfoRequestedEmailHandler> _logger;

        public ApplicationInfoRequestedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository,
            ILogger<ApplicationInfoRequestedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationInfoRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve application to get actual application number and citizen
                var application = await _applicationRepository.GetByIdAsync(notification.ApplicationId, cancellationToken);
                if (application is null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found for info request email", notification.ApplicationId);
                    return;
                }

                // Resolve citizen to get email address
                var citizen = await _userRepository.GetByIdAsync(application.CitizenId, cancellationToken);
                if (citizen is null || string.IsNullOrWhiteSpace(citizen.Email))
                {
                    _logger.LogWarning("Citizen {CitizenId} not found or has no email for info request email", application.CitizenId);
                    return;
                }

                // Resolve permit type to get display name
                var permitTypeName = await _permitTypeRepository.GetNameByIdAsync(application.PermitTypeId, cancellationToken)
                    ?? "Unknown";

                // Use anonymous object because InfoRequestNotification template needs Message
                // which is not a property of ApplicationSummaryDto
                var model = new
                {
                    ApplicationNumber = application.ApplicationNumber,
                    PermitTypeName = permitTypeName,
                    Message = notification.Message
                };

                var body = await _templateRenderer.RenderAsync("InfoRequestNotification", model, cancellationToken);

                await _emailService.SendAsync(
                    citizen.Email,
                    "Additional Information Requested",
                    body,
                    isHtml: false,
                    cancellationToken);

                _logger.LogInformation(
                    "Sent info request email for application {ApplicationId} to {Email}",
                    notification.ApplicationId, citizen.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send info request email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}