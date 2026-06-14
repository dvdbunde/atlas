using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;

namespace ATLAS.Application.Commands.Applications
{
    public class SubmitDraftCommand : IRequest<Unit>
    {
        public Guid ApplicationId { get; set; }
    }

    public class SubmitDraftCommandHandler : IRequestHandler<SubmitDraftCommand, Unit>
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SubmitDraftCommandHandler> _logger;

        public SubmitDraftCommandHandler(
            IApplicationRepository applicationRepository,
            IPermitTypeRepository permitTypeRepository,
            ICurrentUserService currentUserService,
            ILogger<SubmitDraftCommandHandler> logger)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(SubmitDraftCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
            if (application == null)
                throw new ArgumentException($"Application {request.ApplicationId} not found");

            // Verify ownership
            if (!_currentUserService.UserId.HasValue || application.CitizenId != _currentUserService.UserId.Value)
                throw new UnauthorizedAccessException("User can only submit their own applications");

            // Verify status
            if (application.Status != ApplicationStatus.Draft)
                throw new InvalidOperationException("Only draft applications can be submitted");

            // Rule 3: Application must contain at least one FieldValue
            if (!application.FieldValues.Any())
                throw new InvalidOperationException("Application must have at least one field value before submission");

            // Load PermitType to validate fields
            var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);
            if (permitType == null)
                throw new ArgumentException($"Permit type {application.PermitTypeId} not found");

            // Rule 1: Every FieldValue must reference an existing PermitField name
            var fieldNames = permitType.Fields.Select(f => f.Name).ToList();
            foreach (var fieldValue in application.FieldValues)
            {
                if (!fieldNames.Contains(fieldValue.FieldName, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Field '{fieldValue.FieldName}' is not defined in permit type");
            }

            // Rule 2: All required PermitFields must have values
            var requiredFields = permitType.Fields.Where(f => f.IsRequired).Select(f => f.Name).ToList();
            var providedFields = application.FieldValues.Select(f => f.FieldName).ToList();

            foreach (var requiredField in requiredFields)
            {
                if (!providedFields.Contains(requiredField, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Required field '{requiredField}' is missing");

                var fieldValue = application.FieldValues.First(f => f.FieldName.Equals(requiredField, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(fieldValue.Value))
                    throw new InvalidOperationException($"Required field '{requiredField}' must have a value");
            }

            // All validation passed, submit
            application.Submit();
            await _applicationRepository.UpdateAsync(application, cancellationToken);

            _logger.LogInformation("Draft application {ApplicationId} submitted", request.ApplicationId);

            return Unit.Value;
        }
    }
}