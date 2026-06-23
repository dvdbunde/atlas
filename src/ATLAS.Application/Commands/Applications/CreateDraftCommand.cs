using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Commands.Applications
{
    public class CreateDraftCommand : IRequest<Guid>
    {
        public Guid PermitTypeId { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
        public Dictionary<string, string> FieldValues { get; set; } = new();
    }

    public class CreateDraftCommandHandler : IRequestHandler<CreateDraftCommand, Guid>
    {
        private readonly IApplicationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateDraftCommandHandler> _logger;

        public CreateDraftCommandHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            ILogger<CreateDraftCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Guid> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("User must be authenticated to create a draft application.");

            var citizenId = _currentUserService.UserId.Value;
            var application = new Domain.Entities.Application(citizenId, request.PermitTypeId, request.CitizenNotes);

            // Add field values if provided
            var sortOrder = 0;
            foreach (var field in request.FieldValues)
            {
                application.AddFieldValue(field.Key, field.Value, sortOrder++);
            }

            await _repository.AddAsync(application, cancellationToken);
            _logger.LogInformation("Draft application {ApplicationId} created for citizen {CitizenId}", 
                application.Id, citizenId);

            return application.Id;
        }
    }
}