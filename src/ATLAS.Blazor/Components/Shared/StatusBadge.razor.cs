using ATLAS.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Shared;

public partial class StatusBadge : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public ApplicationStatus Status { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}