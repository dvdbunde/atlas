using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.Admin;

/// <summary>
/// Sort options for the Administrator Application Explorer.
/// </summary>
public enum AdminApplicationSortBy
{
    SubmittedDate = 0,
    LastUpdated = 1,
    ApplicationNumber = 2
}

/// <summary>
/// Query to retrieve a paged, filtered, sorted, read-only list of all applications
/// for the Administrator Application Explorer. Administrators have full visibility
/// across all applications regardless of status or assignment.
/// </summary>
public class GetAdminApplicationsQuery : IRequest<AdminApplicationListResult>
{
    /// <summary>Search term — matches application number or citizen name.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Comma-separated status values to include.</summary>
    public string? Status { get; set; }

    /// <summary>Optional permit type filter.</summary>
    public Guid? PermitTypeId { get; set; }

    /// <summary>Earliest submitted date (inclusive).</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Latest submitted date (inclusive).</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Sort field. Defaults to SubmittedDate.</summary>
    public AdminApplicationSortBy SortBy { get; set; } = AdminApplicationSortBy.SubmittedDate;

    /// <summary>When true (default), newest items appear first.</summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>1-based page number.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>Paged result wrapper for the Administrator Application Explorer.</summary>
public class AdminApplicationListResult
{
    public IReadOnlyList<AdminApplicationExplorerDto> Items { get; init; } = Array.Empty<AdminApplicationExplorerDto>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetAdminApplicationsQueryHandler : IRequestHandler<GetAdminApplicationsQuery, AdminApplicationListResult>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermitTypeRepository _permitTypeRepository;

    public GetAdminApplicationsQueryHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IPermitTypeRepository permitTypeRepository)
    {
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
    }

    public async Task<AdminApplicationListResult> Handle(GetAdminApplicationsQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // 1. Fetch all applications (consistent with existing query patterns).
        var applications = (await _applicationRepository.GetAllAsync(cancellationToken)).AsQueryable();

        // 2. Filter by status (comma-separated).
        if (!string.IsNullOrEmpty(request.Status))
        {
            var statusList = request.Status.Split(',')
                .Select(s =>
                {
                    if (Enum.TryParse<ApplicationStatus>(s.Trim(), out var status))
                        return (ApplicationStatus?)status;
                    return null;
                })
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();

            if (statusList.Any())
                applications = applications.Where(a => statusList.Contains(a.Status));
        }

        // 3. Filter by permit type.
        if (request.PermitTypeId.HasValue)
            applications = applications.Where(a => a.PermitTypeId == request.PermitTypeId.Value);

        // 4. Filter by submitted date range.
        if (request.DateFrom.HasValue)
            applications = applications.Where(a => a.SubmittedDate >= request.DateFrom);

        if (request.DateTo.HasValue)
            applications = applications.Where(a => a.SubmittedDate <= request.DateTo);

        // 5. Materialize for in-memory filtering (search by citizen name).
        var materialized = applications.ToList();

        // 6. Search by application number or citizen name.
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            // Try to parse as a number (application number search)
            var matchingUserIds = new HashSet<Guid>();

            // Search users by name to find matching citizen IDs.
            var allUsers = (await _userRepository.GetAllAsync(cancellationToken)).ToList();
            var matchingUsers = allUsers
                .Where(u => u.GetFullName().Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Id)
                .ToHashSet();

            materialized = materialized
                .Where(a =>
                    a.ApplicationNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    matchingUsers.Contains(a.CitizenId))
                .ToList();
        }

        // 7. Sort.
        materialized = request.SortBy switch
        {
            AdminApplicationSortBy.LastUpdated => request.SortDescending
                ? materialized.OrderByDescending(a => a.ReviewedDate ?? a.SubmittedDate).ToList()
                : materialized.OrderBy(a => a.ReviewedDate ?? a.SubmittedDate).ToList(),
            AdminApplicationSortBy.ApplicationNumber => request.SortDescending
                ? materialized.OrderByDescending(a => a.ApplicationNumber).ToList()
                : materialized.OrderBy(a => a.ApplicationNumber).ToList(),
            _ => request.SortDescending
                ? materialized.OrderByDescending(a => a.SubmittedDate).ToList()
                : materialized.OrderBy(a => a.SubmittedDate).ToList()
        };

        // 8. Paginate.
        var totalCount = materialized.Count;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        var paged = materialized
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 9. Enrich with names.
        var dtos = new List<AdminApplicationExplorerDto>();
        foreach (var app in paged)
        {
            var citizen = await _userRepository.GetByIdAsync(app.CitizenId, cancellationToken);
            var permitType = await _permitTypeRepository.GetByIdAsync(app.PermitTypeId, cancellationToken);

            string? assignedOfficerName = null;
            if (app.AssignedOfficerId.HasValue)
            {
                var officer = await _userRepository.GetByIdAsync(app.AssignedOfficerId.Value, cancellationToken);
                assignedOfficerName = officer?.GetFullName();
            }

            dtos.Add(new AdminApplicationExplorerDto
            {
                ApplicationId = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                PermitTypeName = permitType?.Name ?? "Unknown",
                CitizenName = citizen?.GetFullName() ?? "Unknown",
                Status = app.Status,
                AssignedOfficerName = assignedOfficerName,
                SubmittedDate = app.SubmittedDate,
                LastUpdated = app.ReviewedDate ?? app.SubmittedDate
            });
        }

        return new AdminApplicationListResult
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}