using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<Users> Logger { get; set; } = default!;

    private UsersListViewModel _viewModel = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadUsers();
            StateHasChanged();
        }
    }

    private async Task LoadUsers()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetUsersQuery
            {
                SearchTerm = _viewModel.SearchTerm,
                Role = _viewModel.RoleFilter,
                SortBy = _viewModel.SortBy,
                PageNumber = _viewModel.PageNumber,
                PageSize = _viewModel.PageSize
            };

            var result = await Mediator.Send(query);
            _viewModel.Items = result.Items;
            _viewModel.TotalCount = result.TotalCount;
            _viewModel.TotalPages = result.TotalPages;
            _viewModel.PageNumber = result.PageNumber;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load users");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load users. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _viewModel.SearchTerm = e.Value?.ToString() ?? string.Empty;
        _viewModel.PageNumber = 1;
        await LoadUsers();
        StateHasChanged();
    }

    private async Task OnRoleFilterChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.RoleFilter = string.IsNullOrWhiteSpace(raw) ? null : (ATLAS.Domain.Entities.UserRole?)int.Parse(raw);
        _viewModel.PageNumber = 1;
        await LoadUsers();
        StateHasChanged();
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.SortBy = Enum.TryParse<ATLAS.Application.Queries.Admin.UserSortOption>(raw, out var sort)
            ? sort
            : ATLAS.Application.Queries.Admin.UserSortOption.NameAsc;
        _viewModel.PageNumber = 1;
        await LoadUsers();
        StateHasChanged();
    }

    private async Task GoToPage(int page)
    {
        if (page < 1 || page > _viewModel.TotalPages)
            return;
        _viewModel.PageNumber = page;
        await LoadUsers();
        StateHasChanged();
    }
}
