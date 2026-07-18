using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Commands.Applications
{
    public class ResubmitApplicationCommand : ICommand<Unit>
    {
        public Guid ApplicationId { get; set; }
    }

    public class ResubmitApplicationCommandHandler : IRequestHandler<ResubmitApplicationCommand, Unit>
    {
        private readonly IApplicationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMediator _mediator;
        private readonly ILogger<ResubmitApplicationCommandHandler> _logger;
        private readonly IPermitTypeRepository _permitTypeRepository;

        public ResubmitApplicationCommandHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            IMediator mediator,
            ILogger<ResubmitApplicationCommandHandler> logger,
            IPermitTypeRepository permitTypeRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        }

        public async Task<Unit> Handle(ResubmitApplicationCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            if (application == null)
                throw new ArgumentException($"Application {request.ApplicationId} not found");

            // Verify ownership
            if (!_currentUserService.UserId.HasValue || application.CitizenId != _currentUserService.UserId.Value)
                throw new UnauthorizedAccessException("User can only resubmit their own applications");

             // Load permit type to validate requirements
            var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);
            if (permitType == null)
                throw new ArgumentException($"Permit type {application.PermitTypeId} not found");

            // Validate required fields have values
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

            // Validate required documents are still uploaded
            var uploadedDocTypes = application.Documents
                .Select(d => d.DocumentType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingDocs = permitType.DocumentRequirements
                .Where(r => r.IsRequired)
                .Select(r => r.DocumentType)
                .Where(dt => !uploadedDocTypes.Contains(dt))
                .ToList();

            if (missingDocs.Any())
            {
                var message = "The following required documents are still missing:" +
                              Environment.NewLine +
                              string.Join(Environment.NewLine, missingDocs.Select(m => $"- {m}"));
                throw new InvalidOperationException(message);
            }

            // Use existing domain behavior
            application.Resubmit();
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationResubmittedEvent(application.Id, application.CitizenId), cancellationToken);
            
            _logger.LogInformation("Application {ApplicationId} resubmitted", request.ApplicationId);

            return Unit.Value;
        }
    }
}
