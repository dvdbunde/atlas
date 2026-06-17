using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Pages;

public partial class ApplicationCreate : ComponentBase
{
    [Parameter]
    public Guid PermitTypeId { get; set; }

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<ApplicationCreate> Logger { get; set; } = default!;
    
    private ApplicationCreateViewModel _viewModel = new();

    private DynamicFormGenerator _dynamicForm = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadPermitType();
    }

    private async Task LoadPermitType()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = PermitTypeId };
            var result = await Mediator.Send(query);

            if (result is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "The requested permit type was not found.";
            }
            else
            {
                _viewModel.LoadFromDto(result);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load permit type {PermitTypeId}", PermitTypeId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the permit type. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

   private async Task SaveDraft()
    {
        // Trigger validation — DynamicFieldValidator populates field.Errors
        _dynamicForm.Validate();

        // Block save if any validation errors
        if (_viewModel.Fields.Any(f => f.HasErrors))
            return;

        _viewModel.IsSaving = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var fieldValues = _viewModel.Fields
                .ToDictionary(f => f.FieldName, f => f.CurrentValue);

            var command = new CreateDraftCommand
            {
                PermitTypeId = _viewModel.PermitTypeId,
                CitizenNotes = string.Empty,
                FieldValues = fieldValues
            };

            var applicationId = await Mediator.Send(command);

            _viewModel.SaveSuccess = true;
            _viewModel.CreatedApplicationId = applicationId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save draft for permit type {PermitTypeId}", _viewModel.PermitTypeId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to save your draft. Please try again.";
        }
        finally
        {
            _viewModel.IsSaving = false;
        }
    }
}