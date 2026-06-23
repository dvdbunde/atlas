//----------------------
// Application Submitted Email Handler
// Sends confirmation email when application is submitted
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using ATLAS.Domain.Enums;

namespace ATLAS.Infrastructure.EventHandlers
{
    public class ApplicationSubmittedEmailHandler : INotificationHandler<ApplicationSubmittedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ILogger<ApplicationSubmittedEmailHandler> _logger;

        public ApplicationSubmittedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository,
            ILogger<ApplicationSubmittedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationSubmittedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve application to get actual application number and citizen
                var application = await _applicationRepository.GetByIdAsync(notification.ApplicationId, cancellationToken);
                if (application is null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found for submission email", notification.ApplicationId);
                    return;
                }

                // Resolve citizen to get email address
                var citizen = await _userRepository.GetByIdAsync(application.CitizenId, cancellationToken);
                if (citizen is null || string.IsNullOrWhiteSpace(citizen.Email))
                {
                    _logger.LogWarning("Citizen {CitizenId} not found or has no email for submission email", application.CitizenId);
                    return;
                }

                // Resolve permit type to get display name
                var permitTypeName = await _permitTypeRepository.GetNameByIdAsync(application.PermitTypeId, cancellationToken)
                    ?? "Unknown";

                var model = new ApplicationSummaryDto
                {
                    Id = application.Id,
                    ApplicationNumber = application.ApplicationNumber,
                    PermitTypeName = permitTypeName,
                    Status = ApplicationStatus.Submitted,
                    SubmittedDate = application.SubmittedDate,
                    CitizenId = application.CitizenId,
                    PermitTypeId = application.PermitTypeId
                };

                var body = await _templateRenderer.RenderAsync("SubmissionConfirmation", model, cancellationToken);

                await _emailService.SendAsync(
                    citizen.Email,
                    "Application Submitted Successfully",
                    body,
                    isHtml: false,
                    cancellationToken);

                _logger.LogInformation(
                    "Sent submission confirmation email for application {ApplicationId} to {Email}",
                    notification.ApplicationId, citizen.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send submission confirmation email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}