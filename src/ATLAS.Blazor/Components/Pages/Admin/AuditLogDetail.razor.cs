using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class AuditLogDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<AuditLogDetail> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private AuditLogDetailViewModel _viewModel = new();

    protected override async Task OnParametersSetAsync()
    {
        await LoadLog();
    }

    private async Task LoadLog()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.NotFound = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var log = await Mediator.Send(new GetAuditLogDetailQuery { Id = Id });
            _viewModel.AuditLog = log;
            _viewModel.NotFound = log is null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load audit log {AuditLogId}", Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load this audit entry. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private void BackToList() => Navigation.NavigateTo("/admin/audit-logs");
}
