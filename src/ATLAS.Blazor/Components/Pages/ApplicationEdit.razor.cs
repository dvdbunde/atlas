using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Commands.Documents;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;
 
namespace ATLAS.Blazor.Components.Pages;

public partial class ApplicationEdit : ComponentBase
{
    [SupplyParameterFromQuery(Name = "created")]
    public bool? Created { get; set; }

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

    private bool _dataLoaded;

    private ElementReference _submitErrorAlert;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadApplication();
            StateHasChanged();
        }
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

            // Verify the application is in an editable state
            if (application.Status != ApplicationStatus.Draft && application.Status != ApplicationStatus.InfoRequested)
            {
                Navigation.NavigateTo($"/applications/{Id}");
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

            if (Created == true)
            {
                _viewModel.CreatedSuccess = true;
            }
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

    private async Task HandleFileSelected(DynamicFormFieldViewModel field)
    {
        if (field.Type != FieldType.FileUpload || field.SelectedFileContent is null)
            return;

        field.IsUploading = true;
        field.UploadFailed = false;
        field.UploadErrorMessage = null;

        try
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = _viewModel.ApplicationId,
                DocumentType = field.FieldName,
                FileContent = new MemoryStream(field.SelectedFileContent),
                FileName = field.SelectedFileName ?? "document",
                ContentType = GetContentType(field.SelectedFileName ?? ""),
                FileSize = field.SelectedFileContent.Length
            };

            await Mediator.Send(command);

            // Refresh application to show uploaded documents
            var appQuery = new GetApplicationByIdQuery { ApplicationId = _viewModel.ApplicationId };
            var application = await Mediator.Send(appQuery);

            if (application is not null)
            {
                // Reload fields with updated documents
                var permitQuery = new GetPermitTypeByIdQuery { PermitTypeId = _viewModel.PermitTypeId };
                var permitType = await Mediator.Send(permitQuery);
                if (permitType is not null)
                {
                    _viewModel.Load(application, permitType);
                }
            }

            field.SelectedFileName = null;
            field.SelectedFileContent = null;
        }
        catch (UnauthorizedAccessException)
        {
            field.UploadFailed = true;
            field.UploadErrorMessage = "You can only upload to your own applications.";
        }
        catch (InvalidOperationException ex)
        {
            field.UploadFailed = true;
            field.UploadErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Upload failed for field {FieldName}", field.FieldName);
            field.UploadFailed = true;
            field.UploadErrorMessage = "Upload failed. Please try again.";
        }
        finally
        {
            field.IsUploading = false;
        }
    }

    private async Task HandleDocumentDeleted((DynamicFormFieldViewModel Field, Guid DocumentId) args)
    {
        if (args.Field is null || args.DocumentId == Guid.Empty)
            return;

        var field = args.Field;
        
        field.IsDeleting = true;
        field.DeleteFailed = false;
        field.DeleteErrorMessage = null;

        try
        {
            var command = new DeleteDocumentCommand
            {
                ApplicationId = _viewModel.ApplicationId,
                DocumentId = args.DocumentId
            };

            await Mediator.Send(command);

            // Remove from local list
            field.UploadedDocuments.RemoveAll(d => d.Id == args.DocumentId);

            Logger.LogInformation(
                "Document {DocumentId} deleted from application {ApplicationId}",
                args.DocumentId,
                _viewModel.ApplicationId);
        }
        catch (UnauthorizedAccessException)
        {
            field.DeleteFailed = true;
            field.DeleteErrorMessage = "You can only delete documents from your own applications.";
        }
        catch (InvalidOperationException ex)
        {
            field.DeleteFailed = true;
            field.DeleteErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete document {DocumentId} from application {ApplicationId}",
                args.DocumentId, _viewModel.ApplicationId);
            field.DeleteFailed = true;
            field.DeleteErrorMessage = "Failed to delete document. Please try again.";
        }
        finally
        {
            field.IsDeleting = false;
        }
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private void DismissSuccess()
    {
        _viewModel.SaveSuccess = false;
    }

    private void DismissCreatedSuccess()
    {
        _viewModel.CreatedSuccess = false;
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
        catch (InvalidOperationException ex)
        {
            // Submission validation failures (including missing required documents)
            _viewModel.SubmitHasError = true;            
            _viewModel.SubmitErrorMessage = ex.Message;

            // Focus the error alert for screen readers
            _ = Task.Run(async () => 
            {
                await Task.Delay(100); // Wait for render
                await _submitErrorAlert.FocusAsync();
            });

            Logger.LogWarning(ex, "Submission validation failed for application {ApplicationId}", _viewModel.ApplicationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to submit application {ApplicationId}", _viewModel.ApplicationId);
            _viewModel.SubmitHasError = true;
            _viewModel.SubmitErrorMessage = "We were unable to submit your application. Please try again.";

            // Focus the error alert for screen readers
            _ = Task.Run(async () => 
            {
                await Task.Delay(100); // Wait for render
                await _submitErrorAlert.FocusAsync();
            });
        }
        finally
        {
            _viewModel.IsSubmitting = false;
        }
    }

       private async Task ResubmitApplication()
    {
        _viewModel.ResubmitHasError = false;
        _viewModel.ResubmitErrorMessage = null;
        _viewModel.IsResubmitting = true;

        try
        {
            // First save any pending edits
            await SaveDraft();

            // Then resubmit
            await Mediator.Send(new ResubmitApplicationCommand { ApplicationId = _viewModel.ApplicationId });
            _viewModel.ResubmitSuccess = true;
        }
        catch (InvalidOperationException ex)
        {
            // Validation failures (missing required documents, fields, etc.)
            _viewModel.ResubmitHasError = true;
            _viewModel.ResubmitErrorMessage = ex.Message;

            Logger.LogWarning(ex, "Resubmission validation failed for application {ApplicationId}", _viewModel.ApplicationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resubmit application {ApplicationId}", _viewModel.ApplicationId);
            _viewModel.ResubmitHasError = true;
            _viewModel.ResubmitErrorMessage = "We were unable to resubmit the application. Please try again.";
        }
        finally
        {
            _viewModel.IsResubmitting = false;
        }
    }
}