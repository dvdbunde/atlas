using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Permit Selection page.
/// Wraps PermitTypeSummaryDto for UI presentation.
/// </summary>
public class PermitSelectionViewModel
{
    public List<PermitTypeCardViewModel> PermitTypes { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsEmpty => !IsLoading && !HasError && PermitTypes.Count == 0;
}

/// <summary>
/// Card-level view model for a single permit type.
/// </summary>
public class PermitTypeCardViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Fee { get; init; }
    public bool HasCost => Fee > 0;
    public string NavigationUrl => $"/applications/create/{Id}";

    public static PermitTypeCardViewModel FromDto(PermitTypeSummaryDto dto)
    {
        return new PermitTypeCardViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Fee = dto.Fee
        };
    }
}