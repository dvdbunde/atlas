using ATLAS.Application.Commands.Applications;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
 
namespace ATLAS.Blazor.Components.Pages;

public partial class ApplicationEdit : ComponentBase
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<ApplicationEdit> Logger { get; set; } = default!;

    private ApplicationEditViewModel _viewModel = new();
    private DynamicFormGenerator _dynamicForm = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;


    protected override async Task OnInitializedAsync()
    {
        await LoadApplication();
    }

    private async Task LoadApplication()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var appQuery = new GetApplicationByIdQuery { ApplicationId = Id };
            var application = await Mediator.Send(appQuery);

            if (application is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "Application not found. It may have been removed or you may not have permission to view it.";
                return;
            }

            if (application.Status != Domain.Enums.ApplicationStatus.Draft)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = $"This application is in \"{application.Status}\" status and cannot be edited. Only draft applications can be modified.";
                return;
            }

            var permitQuery = new GetPermitTypeByIdQuery { PermitTypeId = application.PermitTypeId };
            var permitType = await Mediator.Send(permitQuery);

            if (permitType is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "The permit type for this application is no longer available.";
                return;
            }

            _viewModel.Load(application, permitType);
        }   
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load application {ApplicationId}", Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the application. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task SaveDraft()
    {
        _dynamicForm.Validate();

        if (_viewModel.Fields.Any(f => f.HasErrors))
            return;

        _viewModel.IsSaving = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var fieldValues = _viewModel.Fields
                .ToDictionary(f => f.FieldName, f => f.CurrentValue);

            var command = new UpdateDraftCommand
            {
                ApplicationId = _viewModel.ApplicationId,
                CitizenNotes = string.Empty,
                FieldValues = fieldValues
            };

            await Mediator.Send(command);

            _viewModel.SaveSuccess = true;

            Logger.LogInformation(
                "Draft application {ApplicationId} updated successfully",
                _viewModel.ApplicationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save application {ApplicationId}", _viewModel.ApplicationId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to save your changes. Please try again.";
        }
        finally
        {
            _viewModel.IsSaving = false;
        }
    }

    private void DismissSuccess()
    {
        _viewModel.SaveSuccess = false;
    }

    private async Task SubmitApplication()
    {
        // Validate before submit
        _dynamicForm.Validate();

        if (_viewModel.Fields.Any(f => f.HasErrors))
            return;

        _viewModel.IsSubmitting = true;
        _viewModel.SubmitHasError = false;
        _viewModel.SubmitErrorMessage = null;

        try
        {
            var command = new SubmitDraftCommand
            {
                ApplicationId = _viewModel.ApplicationId
            };

            await Mediator.Send(command);

            Logger.LogInformation(
                "Application {ApplicationId} submitted successfully",
                _viewModel.ApplicationId);

            Navigation.NavigateTo($"/applications/confirmation/{_viewModel.ApplicationId}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to submit application {ApplicationId}", _viewModel.ApplicationId);
            _viewModel.SubmitHasError = true;
            _viewModel.SubmitErrorMessage = "We were unable to submit your application. Please fix any validation errors and try again.";
        }
        finally
        {
            _viewModel.IsSubmitting = false;
        }
    }
}