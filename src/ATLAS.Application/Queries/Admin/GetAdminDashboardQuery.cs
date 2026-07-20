using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Queries.Admin;

/// <summary>
/// Query to retrieve lightweight, high-level summary counts for the Administration
/// dashboard. Returns only aggregate counts — never full entities or lists.
/// Reuses existing read models (permit types, applications, users) and exposes a
/// placeholder count for email templates that are not yet implemented.
/// </summary>
public class GetAdminDashboardQuery : IRequest<AdminDashboardDto>
{
    // No parameters — dashboard is a global, role-scoped overview.
}

/// <summary>Read model for the Administration dashboard summary.</summary>
public class AdminDashboardDto
{
    /// <summary>Total number of permit types (active and inactive).</summary>
    public int PermitTypeCount { get; init; }

    /// <summary>Total number of permit applications across all statuses.</summary>
    public int ApplicationCount { get; init; }

    /// <summary>Total number of officers (users with the Officer role).</summary>
    public int OfficerCount { get; init; }

    /// <summary>
    /// Number of active email templates. Placeholder until Email Template management
    /// is implemented in a later Milestone 8 phase. Returns 0 for now.
    /// </summary>
    public int ActiveEmailTemplateCount { get; init; }
}

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IPermitTypeRepository _permitTypeRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;

    public GetAdminDashboardQueryHandler(
        IPermitTypeRepository permitTypeRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository)
    {
        _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var permitTypes = await _permitTypeRepository.GetAllAsync(cancellationToken);
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        var officers = await _userRepository.GetByRoleAsync(ATLAS.Domain.Entities.UserRole.Officer, cancellationToken);

        return new AdminDashboardDto
        {
            PermitTypeCount = permitTypes.Count(),
            ApplicationCount = applications.Count(),
            OfficerCount = officers.Count(),
            // Placeholder: Email Template management is implemented in a later phase.
            ActiveEmailTemplateCount = 0
        };
    }
}
