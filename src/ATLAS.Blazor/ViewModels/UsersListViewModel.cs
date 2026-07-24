using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;
using ATLAS.Domain.Entities;

namespace ATLAS.Blazor.ViewModels;

public class UsersListViewModel
{
    public IReadOnlyList<UserSummaryDto> Items { get; set; } = Array.Empty<UserSummaryDto>();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public string SearchTerm { get; set; } = string.Empty;
    public UserRole? RoleFilter { get; set; }
    public UserSortOption SortBy { get; set; } = UserSortOption.NameAsc;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;
}
