using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class UserDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<UserDetail> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private UserDetailViewModel _viewModel = new();

    protected override async Task OnParametersSetAsync()
    {
        await LoadUser();
    }

    private async Task LoadUser()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.NotFound = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var user = await Mediator.Send(new GetUserByIdQuery { UserId = Id });
            _viewModel.User = user;
            _viewModel.NotFound = user is null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load user {UserId}", Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load this user. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private void BackToList() => Navigation.NavigateTo("/admin/users");
}
