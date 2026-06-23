using ATLAS.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Shared;

public partial class ApplicationTimeline : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<TimelineEntryViewModel> Entries { get; set; } = Array.Empty<TimelineEntryViewModel>();
}