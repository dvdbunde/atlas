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

        public ResubmitApplicationCommandHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            IMediator mediator,
            ILogger<ResubmitApplicationCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Use existing domain behavior
            application.Resubmit();
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new ApplicationResubmittedEvent(application.Id, application.CitizenId), cancellationToken);
            
            _logger.LogInformation("Application {ApplicationId} resubmitted", request.ApplicationId);

            return Unit.Value;
        }
    }
}
