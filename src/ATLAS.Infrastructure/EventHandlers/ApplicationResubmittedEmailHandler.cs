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
    public class ApplicationResubmittedEmailHandler : INotificationHandler<ApplicationResubmittedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ILogger<ApplicationResubmittedEmailHandler> _logger;

        public ApplicationResubmittedEmailHandler(
            IEmailService emailService,
            IEmailTemplateRenderer templateRenderer,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository,
            ILogger<ApplicationResubmittedEmailHandler> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(ApplicationResubmittedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(notification.ApplicationId, cancellationToken);
                if (application is null)
                {
                    _logger.LogWarning("Application {ApplicationId} not found for resubmission email", notification.ApplicationId);
                    return;
                }

                var citizen = await _userRepository.GetByIdAsync(application.CitizenId, cancellationToken);
                if (citizen is null || string.IsNullOrWhiteSpace(citizen.Email))
                {
                    _logger.LogWarning("Citizen {CitizenId} not found or has no email for resubmission email", application.CitizenId);
                    return;
                }

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

                var body = await _templateRenderer.RenderAsync("ResubmissionConfirmation", model, cancellationToken);

                await _emailService.SendAsync(
                    citizen.Email,
                    "Application Resubmitted Successfully",
                    body,
                    isHtml: false,
                    cancellationToken);

                _logger.LogInformation(
                    "Sent resubmission confirmation email for application {ApplicationId} to {Email}",
                    notification.ApplicationId, citizen.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send resubmission confirmation email for application {ApplicationId}", notification.ApplicationId);
            }
        }
    }
}