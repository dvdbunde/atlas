using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Queries.Applications;

/// <summary>
/// Sort options for the officer dashboard. Designed for extension (add fields later).
/// </summary>
public enum OfficerDashboardSortBy
{
    SubmittedDate = 0,
    LastUpdated = 1
}

/// <summary>
/// Query to retrieve a paged, filtered, sorted summary of applications that have
/// entered the officer workflow (Submitted, UnderReview, InfoRequested).
/// Returns summary DTOs only — never the full Application aggregate.
/// </summary>
public class GetOfficerDashboardQuery : IRequest<OfficerDashboardResult>
{
    /// <summary>
    /// Statuses to include. When null/empty, defaults to the officer-workflow statuses.
    /// Any value outside the officer workflow is ignored by the handler.
    /// </summary>
    public List<ApplicationStatus>? Statuses { get; set; }

    /// <summary>Optional permit type filter.</summary>
    public Guid? PermitTypeId { get; set; }

    /// <summary>Sort field. Defaults to LastUpdated.</summary>
    public OfficerDashboardSortBy SortBy { get; set; } = OfficerDashboardSortBy.LastUpdated;

    /// <summary>When true (default), newest items appear first.</summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>1-based page number.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>Paged result wrapper for the officer dashboard.</summary>
public class OfficerDashboardResult
{
    public IReadOnlyList<OfficerDashboardDto> Items { get; init; } = Array.Empty<OfficerDashboardDto>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetOfficerDashboardQueryHandler : IRequestHandler<GetOfficerDashboardQuery, OfficerDashboardResult>
{
    // Statuses that belong to the officer workflow (excludes Draft/Approved/Rejected).
    private static readonly ApplicationStatus[] OfficerWorkflowStatuses =
    {
        ApplicationStatus.Submitted,
        ApplicationStatus.UnderReview,
        ApplicationStatus.InfoRequested
    };

    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermitTypeRepository _permitTypeRepository;

    public GetOfficerDashboardQueryHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IPermitTypeRepository permitTypeRepository)
    {
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
    }

    public async Task<OfficerDashboardResult> Handle(GetOfficerDashboardQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // 1. Effective status filter (intersect with officer-workflow statuses).
        var statusFilter = (request.Statuses != null && request.Statuses.Count > 0)
            ? request.Statuses.Where(s => OfficerWorkflowStatuses.Contains(s)).Distinct().ToList()
            : OfficerWorkflowStatuses.ToList();

        // 2. Fetch all applications (repository returns IEnumerable) — consistent with GetApplicationsQuery.
        var applications = (await _applicationRepository.GetAllAsync(cancellationToken)).AsQueryable();

        // 3. Scope to officer-workflow statuses.
        applications = applications.Where(a => statusFilter.Contains(a.Status));

        // 4. Permit type filter.
        if (request.PermitTypeId.HasValue)
            applications = applications.Where(a => a.PermitTypeId == request.PermitTypeId.Value);

        // 5. Sorting (newest first by default).
        applications = request.SortBy switch
        {
            OfficerDashboardSortBy.SubmittedDate => request.SortDescending
                ? applications.OrderByDescending(a => a.SubmittedDate)
                : applications.OrderBy(a => a.SubmittedDate),
            _ => request.SortDescending
                ? applications.OrderByDescending(a => a.ReviewedDate ?? a.SubmittedDate)
                : applications.OrderBy(a => a.ReviewedDate ?? a.SubmittedDate)
        };

        // 6. Pagination (in-memory; repository has no paged method yet — mirrors existing query pattern).
        var totalCount = applications.Count();
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        var paged = applications
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 7. Project to summary DTOs with enrichment (no domain entities returned).
        var dtos = new List<OfficerDashboardDto>();
        foreach (var app in paged)
        {
            var citizen = await _userRepository.GetByIdAsync(app.CitizenId, cancellationToken);
            var permitType = await _permitTypeRepository.GetByIdAsync(app.PermitTypeId, cancellationToken);

            // Assigned officer (if any) — derived read-only from explicit assignment state.
            string? assignedOfficerName = null;
            if (app.AssignedOfficerId.HasValue)
            {
                var officer = await _userRepository.GetByIdAsync(app.AssignedOfficerId.Value, cancellationToken);
                assignedOfficerName = officer?.GetFullName();
            }

            // Required-document completeness.
            var requiredDocTypes = (permitType?.DocumentRequirements ?? Enumerable.Empty<DocumentRequirement>())
                .Where(d => d.IsRequired)
                .Select(d => d.DocumentType)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uploadedDocTypes = app.Documents.Select(d => d.DocumentType).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allRequiredUploaded = requiredDocTypes.Count == 0 || requiredDocTypes.All(uploadedDocTypes.Contains);

            dtos.Add(new OfficerDashboardDto
            {
                ApplicationId = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                PermitTypeName = permitType?.Name ?? "Unknown",
                Status = app.Status,
                CitizenName = citizen?.GetFullName() ?? "Unknown",
                SubmittedDate = app.SubmittedDate,
                LastUpdated = app.ReviewedDate ?? app.SubmittedDate,
                AssignedOfficerName = assignedOfficerName,
                AssignedOfficerId = app.AssignedOfficerId,
                DocumentCount = app.Documents.Count,
                AllRequiredDocumentsUploaded = allRequiredUploaded
            });
        }

        return new OfficerDashboardResult
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}