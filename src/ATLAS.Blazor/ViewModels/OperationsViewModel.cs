using ATLAS.Application.Queries.Admin;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Operations overview page.
/// Encapsulates operational state and loading/error states.
/// </summary>
public class OperationsViewModel
{
    public OperationsOverviewDto? Overview { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public string OverallStatus =>
        Overview?.OverallHealth ?? "Unknown";

    public IReadOnlyList<KeyValuePair<string, long>> TransitionsOrdered =>
        Overview?.ApplicationTransitions?
            .OrderBy(kvp => kvp.Key)
            .ToList()
            .AsReadOnly()
        ?? (IReadOnlyList<KeyValuePair<string, long>>)Array.Empty<KeyValuePair<string, long>>();

    public long EmailSuccessCount =>
        Overview?.EmailSends?.TryGetValue("success", out var s) == true ? s : 0;

    public long EmailFailureCount =>
        Overview?.EmailSends?.TryGetValue("failure", out var f) == true ? f : 0;

    public DateTime CapturedAtLocal =>
        Overview?.CapturedAtUtc ?? DateTime.MinValue;
}