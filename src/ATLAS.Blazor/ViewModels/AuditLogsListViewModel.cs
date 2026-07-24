using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.AuditLogs;

namespace ATLAS.Blazor.ViewModels;

public class AuditLogsListViewModel
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = Array.Empty<AuditLogDto>();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public string SearchTerm { get; set; } = string.Empty;
    public string? ActionFilter { get; set; }
    public string? EntityTypeFilter { get; set; }
    public AuditLogSortOptionDto SortBy { get; set; } = AuditLogSortOptionDto.TimestampDesc;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;
}
