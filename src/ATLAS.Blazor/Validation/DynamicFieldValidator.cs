using ATLAS.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ATLAS.Blazor.Validation;

public class DynamicFieldValidator : ComponentBase, IDisposable
{
    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<DynamicFormFieldViewModel> Fields { get; set; } = Array.Empty<DynamicFormFieldViewModel>();

    private ValidationMessageStore? _messageStore;

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(DynamicFieldValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. " +
                $"Ensure it is placed inside an EditForm component.");
        }

        _messageStore = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += HandleValidationRequested;
        CurrentEditContext.OnFieldChanged += HandleFieldChanged;
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        _messageStore!.Clear();

        foreach (var field in Fields)
        {
            field.Errors.Clear();
            ValidateField(field);
        }
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        var field = Fields.FirstOrDefault(f => f.FieldName == args.FieldIdentifier.FieldName);
        if (field is null)
        {
            return;
        }

        _messageStore!.Clear(args.FieldIdentifier);
        field.Errors.Clear();

        ValidateField(field);
    }

    private void ValidateField(DynamicFormFieldViewModel field)
    {
        var fieldIdentifier = new FieldIdentifier(
            CurrentEditContext!.Model,
            field.FieldName);

        if (field.IsRequired && string.IsNullOrWhiteSpace(field.CurrentValue))
        {
            var message = $"{field.Label} is required.";
            _messageStore!.Add(fieldIdentifier, message);
            field.Errors.Add(message);
        }
    }

    public void Dispose()
    {
        _messageStore?.Clear();

        if (CurrentEditContext is not null)
        {
            CurrentEditContext.OnValidationRequested -= HandleValidationRequested;
            CurrentEditContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}
