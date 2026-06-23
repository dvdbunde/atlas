using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using ATLAS.Domain;

namespace ATLAS.Application.Commands.Applications
{
    public class UpdateDraftCommand : IRequest<Unit>
    {
        public Guid ApplicationId { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
        public Dictionary<string, string> FieldValues { get; set; } = new();
    }

    public class UpdateDraftCommandHandler : IRequestHandler<UpdateDraftCommand, Unit>
    {
        private readonly Domain.Interfaces.IApplicationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateDraftCommandHandler> _logger;

        public UpdateDraftCommandHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            ILogger<UpdateDraftCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(UpdateDraftCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            if (application == null)
                throw new ArgumentException($"Application {request.ApplicationId} not found");

            // Verify ownership
            if (!_currentUserService.UserId.HasValue || application.CitizenId != _currentUserService.UserId.Value)
                throw new UnauthorizedAccessException("User can only update their own applications");

            // Verify status
            if (application.Status != ApplicationStatus.Draft)
                throw new InvalidOperationException("Only draft applications can be updated");

            // Update citizen notes
            application.UpdateNotes(request.CitizenNotes);

            // Update field values
            foreach (var field in request.FieldValues)
            {
                try
                {
                    application.UpdateFieldValue(field.Key, field.Value);
                }
                catch (DomainException)
                {
                    // Field doesn't exist, add it
                    application.AddFieldValue(field.Key, field.Value, 0);
                }
            }

            await _repository.UpdateAsync(application, cancellationToken);
            _logger.LogInformation("Draft application {ApplicationId} updated", request.ApplicationId);

            return Unit.Value;
        }
    }
}
