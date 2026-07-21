using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class PermitTypeCreate : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<PermitTypeCreate> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly PermitTypeCreateViewModel _viewModel = new();

    private async Task Create()
    {
        _viewModel.IsSaving = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var command = new CreatePermitTypeCommand
            {
                Name = _viewModel.Name,
                Description = _viewModel.Description,
                Fee = _viewModel.Fee
            };

            var newId = await Mediator.Send(command);
            Navigation.NavigateTo($"/admin/permit-types/{newId}/designer");
        }
        catch (FluentValidation.ValidationException ex)
        {
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Please correct the highlighted fields.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create permit type");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to create the permit type. Please try again later.";
        }
        finally
        {
            _viewModel.IsSaving = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/admin/permit-types");
    }
}
