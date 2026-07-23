using System.Security.Claims;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypeSettingsTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private static PermitTypeDto SampleDto(Guid id, bool active = true) => new()
    {
        Id = id,
        Name = "Building Permit",
        Description = "Construction permit",
        Fee = 100m,
        IsActive = active
    };

    private void SetupAuth(Guid adminId)
    {
        var claims = new List<Claim> { new Claim("oid", adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(user);
        var provider = new TestAuthStateProvider(authState);
        Services.AddSingleton<AuthenticationStateProvider>(provider);
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;
        public TestAuthStateProvider(AuthenticationState state) => _state = state;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var id = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).Returns(tcs.Task);
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(Guid.NewGuid());

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

  
    [Fact]
    public void Should_RenderFeeAndActiveControls_WhenLoaded()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(Guid.NewGuid());

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find("#pt-fee"));
        Assert.Contains("Active", cut.Markup);   // status badge, not the old toggle
        Assert.Contains("Deactivate", cut.Markup);
    }

    [Fact]
    public void Should_NotShowDeactivate_WhenInactive()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id, active: false));
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(Guid.NewGuid());

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.Contains("Activate", cut.Markup);
        Assert.DoesNotContain("Deactivate", cut.Markup);
    }

    [Fact]
    public async Task Should_SendUpdateCommand_OnSave()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePermitTypeCommand>(), default))
            .ReturnsAsync(true);
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(Guid.NewGuid());

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.Find("#pt-fee").Change("250");
        cut.Find("button.btn-primary").Click();

        _mediatorMock.Verify(m => m.Send(It.Is<UpdatePermitTypeCommand>(c => c.PermitTypeId == id && c.Fee == 250m), default), Times.Once);
    }

    [Fact]
    public async Task Should_SendDeactivateCommand_WithAdminId()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeactivatePermitTypeCommand>(), default))
            .ReturnsAsync(true);
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(adminId);

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.Find("button.btn-outline-warning").Click();

        _mediatorMock.Verify(m => m.Send(It.Is<DeactivatePermitTypeCommand>(c => c.PermitTypeId == id && c.DeactivatedByAdminId == adminId), default), Times.Once);
    }

    [Fact]
    public void Should_ShowNotFound_WhenDtoIsNull()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync((PermitTypeDto?)null);
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(Guid.NewGuid());

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".alert-warning"));
    }

    [Fact]
    public async Task Should_SendActivateCommand_WithAdminId()
    {
        var id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id, active: false));
        _mediatorMock.Setup(m => m.Send(It.IsAny<ActivatePermitTypeCommand>(), default))
            .ReturnsAsync(true);
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth(adminId);

        var cut = Render<PermitTypeSettings>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.Find("button.btn-outline-success").Click();

        _mediatorMock.Verify(m => m.Send(
            It.Is<ActivatePermitTypeCommand>(c => c.PermitTypeId == id && c.ActivatedByAdminId == adminId),
            default), Times.Once);
    }
}
