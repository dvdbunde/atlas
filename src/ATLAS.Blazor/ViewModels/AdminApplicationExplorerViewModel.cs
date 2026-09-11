using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;

namespace ATLAS.Blazor.ViewModels;

public class AdminApplicationExplorerViewModel
{
    public IReadOnlyList<AdminApplicationExplorerDto> Items { get; set; } = Array.Empty<AdminApplicationExplorerDto>();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public string SearchTerm { get; set; } = string.Empty;
    public string? StatusFilter { get; set; }
    public Guid? PermitTypeIdFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public AdminApplicationSortBy SortBy { get; set; } = AdminApplicationSortBy.LastUpdated;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;

    // Filter options (loaded from queries)
    public List<PermitTypeFilterOption> PermitTypes { get; set; } = new();
}