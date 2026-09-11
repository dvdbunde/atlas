using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Commands.Applications;

/// <summary>
/// "Release assignment" — the officer identity is resolved from the authenticated user.
/// The command carries NO arbitrary Officer ID. The currently assigned officer
/// relinquishes their own assignment, returning the application to Submitted.
/// </summary>
public class ReleaseApplicationCommand : ICommand<bool>
{
    public Guid ApplicationId { get; set; }
}

public class ReleaseApplicationCommandHandler : IRequestHandler<ReleaseApplicationCommand, bool>
{
    private readonly IApplicationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public ReleaseApplicationCommandHandler(
        IApplicationRepository repository,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<bool> Handle(ReleaseApplicationCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to release an application.");

        var officerId = _currentUserService.UserId.Value;
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return false;

        // Aggregate owns all release rules (status + assignment ownership).
        application.ReleaseAssignment(officerId);
        application.Touch(); // Update ModifiedDate to reflect the persisted change
        await _repository.UpdateAsync(application, cancellationToken);

        // Single audit path: the event handler writes the AuditLog.
        // (Mirrors Assign/Approve/Reject/RequestInfo — manual publish, no second source.)
        await _mediator.Publish(new ApplicationReleasedEvent(application.Id), cancellationToken);

        return true;
    }
}
