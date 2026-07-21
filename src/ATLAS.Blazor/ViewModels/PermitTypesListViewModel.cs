using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;

namespace ATLAS.Blazor.ViewModels;

public class PermitTypesListViewModel
{
    public IReadOnlyList<PermitTypeSummaryDto> Items { get; set; } = Array.Empty<PermitTypeSummaryDto>();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public string SearchTerm { get; set; } = string.Empty;
    public bool ActiveOnly { get; set; }
    public bool InactiveOnly { get; set; }
    public PermitTypeSortOption SortBy { get; set; } = PermitTypeSortOption.NameAsc;

    public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;
}
