using ATLAS.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ATLAS.Blazor.Components.Shared;

public partial class DocumentRequirementCard : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public DynamicFormFieldViewModel Field { get; set; } = default!;

    [Parameter]
    public bool AllowFileUpload { get; set; } = true;

    [Parameter]
    public EventCallback<DynamicFormFieldViewModel> OnFileSelected { get; set; }

    [Parameter]
    public EventCallback<(DynamicFormFieldViewModel Field, Guid DocumentId)> OnDocumentDeleted { get; set; }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.GetMultipleFiles().FirstOrDefault();
        if (file is null)
            return;

        Field.SelectedFileName = file.Name;
        Field.CurrentValue = file.Name;

        // Read file content into memory for upload
        using var stream = file.OpenReadStream(maxAllowedSize: 30 * 1024 * 1024); // 30MB buffer
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Field.SelectedFileContent = memoryStream.ToArray();

        // Notify parent to trigger upload
        await OnFileSelected.InvokeAsync(Field);
    }
}