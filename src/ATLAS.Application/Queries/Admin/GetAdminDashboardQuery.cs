using ATLAS.Application.DTOs;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Queries.Admin;

/// <summary>
/// Query to retrieve the Administration dashboard summary for M12-008.
/// Returns only aggregate counts and lightweight summaries — never full entities
/// or lists. Reuses existing read models (permit types, applications, users,
/// email templates) and reuses the existing audit-log and operations queries so
/// the dashboard reflects the same underlying infrastructure as those pages.
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

    /// <summary>Number of active permit types.</summary>
    public int ActivePermitTypeCount { get; init; }

    /// <summary>Number of inactive permit types.</summary>
    public int InactivePermitTypeCount { get; init; }

    /// <summary>Total number of permit applications across all statuses.</summary>
    public int ApplicationCount { get; init; }

    /// <summary>
    /// Application counts by status (Draft, Submitted, UnderReview, InfoRequested,
    /// Resubmitted, Approved, Rejected). Zero-count statuses are omitted.
    /// </summary>
    public IReadOnlyDictionary<ApplicationStatus, int> ApplicationStatusCounts { get; init; }
        = new Dictionary<ApplicationStatus, int>();

    /// <summary>Total number of users (Citizen + Officer + Admin).</summary>
    public int UserCount { get; init; }

    /// <summary>Total number of officers (users with the Officer role).</summary>
    public int OfficerCount { get; init; }

    /// <summary>Total number of admins (users with the Admin role).</summary>
    public int AdminCount { get; init; }

    /// <summary>Total number of citizens (users with the Citizen role).</summary>
    public int CitizenCount { get; init; }

    /// <summary>Number of audit-log events recorded in the last 24 hours.</summary>
    public int AuditLogEventsLast24Hours { get; init; }

    /// <summary>UTC timestamp of the most recent audit-log event, if any.</summary>
    public DateTime? LatestAuditEventUtc { get; init; }

    /// <summary>
    /// Total number of email templates. The existing Email Template feature has no
    /// active/inactive distinction, so this is a total only.
    /// </summary>
    public int EmailTemplateCount { get; init; }

    /// <summary>Current overall system health (Healthy | Degraded | Unhealthy), or null when unavailable.</summary>
    public string? OverallHealth { get; init; }

    /// <summary>UTC timestamp when the Operations health snapshot was captured, if available.</summary>
    public DateTime? HealthCheckedAtUtc { get; init; }
}

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    // The defined dashboard status set in lifecycle order. Applications renders
    // only non-zero statuses in this exact order.
    private static readonly ApplicationStatus[] DashboardStatusOrder =
    {
        ApplicationStatus.Draft,
        ApplicationStatus.Submitted,
        ApplicationStatus.UnderReview,
        ApplicationStatus.InfoRequested,
        ApplicationStatus.Resubmitted,
        ApplicationStatus.Approved,
        ApplicationStatus.Rejected
    };

    private readonly IPermitTypeRepository _permitTypeRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailTemplateStore _emailTemplateStore;
    private readonly IMediator _mediator;

    public GetAdminDashboardQueryHandler(
        IPermitTypeRepository permitTypeRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IEmailTemplateStore emailTemplateStore,
        IMediator mediator)
    {
        _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _emailTemplateStore = emailTemplateStore ?? throw new ArgumentNullException(nameof(emailTemplateStore));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var permitTypes = await _permitTypeRepository.GetAllAsync(cancellationToken);
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        var officers = await _userRepository.GetByRoleAsync(UserRole.Officer, cancellationToken);
        var admins = await _userRepository.GetByRoleAsync(UserRole.Admin, cancellationToken);
        var citizens = await _userRepository.GetByRoleAsync(UserRole.Citizen, cancellationToken);
        var templateNames = await _emailTemplateStore.GetTemplateNamesAsync(cancellationToken);

        // Reuse the existing audit-log query for a last-24-hours event count and
        // latest-event recency (no redundant audit data-access path). Best-effort:
        // if audit retrieval fails, the dashboard still renders the other blocks.
        int auditLast24h = 0;
        DateTime? latestAuditUtc = null;
        try
        {
            var auditSince = DateTime.UtcNow.AddHours(-24);
            var auditResult = await _mediator.Send(new GetAuditLogsQuery
            {
                DateFrom = auditSince,
                SortBy = AuditLogSortOptionDto.TimestampDesc,
                PageNumber = 1,
                PageSize = 1
            }, cancellationToken);
            auditLast24h = auditResult.TotalCount;
            latestAuditUtc = auditResult.Items.FirstOrDefault()?.Timestamp;
        }
        catch (OperationCanceledException)
        {
            // Cancellation must always propagate; never swallow it.
            throw;
        }
        catch (Exception)
        {
            // Audit activity is a supporting metric; leave it as zero/unknown
            // rather than failing the whole dashboard.
        }

        // Reuse the existing operations query so the dashboard uses the same
        // underlying health determination as the Operations page. Best-effort too.
        string? overallHealth = null;
        DateTime? healthCheckedAtUtc = null;
        try
        {
            var operations = await _mediator.Send(new GetOperationsOverviewQuery(), cancellationToken);
            overallHealth = operations.OverallHealth;
            healthCheckedAtUtc = operations.HealthUnavailable ? null : (DateTime?)operations.CapturedAtUtc;
        }
        catch (OperationCanceledException)
        {
            // Cancellation must always propagate; never swallow it.
            throw;
        }
        catch (Exception)
        {
            // Health retrieval is supporting; leave unavailable without failing the dashboard.
        }

        // Build status counts in the defined lifecycle order (Draft → Submitted →
        // Under Review → Info Requested → Resubmitted → Approved → Rejected). Only
        // statuses with a non-zero count are included. Iterating the lifecycle list
        // (rather than the raw group enumeration) makes the dashboard rendering
        // deterministic regardless of dictionary enumeration order.
        var statusCounts = new Dictionary<ApplicationStatus, int>();
        foreach (var status in DashboardStatusOrder)
        {
            var count = applications.Count(a => a.Status == status);
            if (count > 0)
                statusCounts[status] = count;
        }

        var userCount = citizens.Count() + officers.Count() + admins.Count();

        return new AdminDashboardDto
        {
            PermitTypeCount = permitTypes.Count(),
            ActivePermitTypeCount = permitTypes.Count(p => p.IsActive),
            InactivePermitTypeCount = permitTypes.Count(p => !p.IsActive),
            ApplicationCount = applications.Count(),
            ApplicationStatusCounts = statusCounts,
            UserCount = userCount,
            OfficerCount = officers.Count(),
            AdminCount = admins.Count(),
            CitizenCount = citizens.Count(),
            AuditLogEventsLast24Hours = auditLast24h,
            LatestAuditEventUtc = latestAuditUtc,
            EmailTemplateCount = templateNames.Count,
            OverallHealth = overallHealth,
            HealthCheckedAtUtc = healthCheckedAtUtc
        };
    }
}