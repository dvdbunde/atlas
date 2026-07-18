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
/// "Assign to me" — the officer identity is resolved from the authenticated user.
/// The command carries NO arbitrary Officer ID.
/// </summary>
public class AssignApplicationToMeCommand : ICommand<bool>
{
    public Guid ApplicationId { get; set; }
}

public class AssignApplicationToMeCommandHandler : IRequestHandler<AssignApplicationToMeCommand, bool>
{
    private readonly IApplicationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public AssignApplicationToMeCommandHandler(
        IApplicationRepository repository,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<bool> Handle(AssignApplicationToMeCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!_currentUserService.UserId.HasValue)
            throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to assign an application.");

        var officerId = _currentUserService.UserId.Value;
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return false;

        // Idempotency guard at handler level: do not publish a duplicate event
        // when the application is already assigned to the current officer.
        var wasAlreadyAssignedToMe = application.AssignedOfficerId == officerId;

        application.AssignToOfficer(officerId);   // aggregate owns all assignment rules
        await _repository.UpdateAsync(application, cancellationToken);

        // Single audit path: the event handler writes the AuditLog.
        // (Mirrors Approve/Reject/RequestInfo — manual publish, no second source.)
        if (!wasAlreadyAssignedToMe)
            await _mediator.Publish(new ApplicationAssignedToOfficerEvent(application.Id, officerId), cancellationToken);

        return true;
    }
}