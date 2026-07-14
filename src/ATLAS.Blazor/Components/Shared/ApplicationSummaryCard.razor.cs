using ATLAS.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Shared;

public partial class ApplicationSummaryCard : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public OfficerApplicationCardViewModel Application { get; set; } = default!;
}