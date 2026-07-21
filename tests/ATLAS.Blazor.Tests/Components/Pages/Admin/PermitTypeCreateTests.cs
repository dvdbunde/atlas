using System.Security.Claims;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypeCreateTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public PermitTypeCreateTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth();
    }

    private void SetupAuth()
    {
        var claims = new List<Claim> { new Claim("oid", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);
        var provider = new TestAuthStateProvider(new AuthenticationState(user));
        Services.AddSingleton<AuthenticationStateProvider>(provider);
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;
        public TestAuthStateProvider(AuthenticationState state) => _state = state;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    [Fact]
    public void Should_RenderForm_WhenLoaded()
    {
        var cut = Render<PermitTypeCreate>();

        Assert.NotNull(cut.Find("#ptc-name"));
        Assert.NotNull(cut.Find("#ptc-description"));
        Assert.NotNull(cut.Find("#ptc-fee"));
    }

    [Fact]
    public void Should_NavigateToDesigner_OnSuccessfulCreate()
    {
        var newId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePermitTypeCommand>(), default))
            .ReturnsAsync(newId);

        var cut = Render<PermitTypeCreate>();
        var nav = Services.GetRequiredService<NavigationManager>();

        cut.Find("#ptc-name").Change("Building Permit");
        cut.Find("#ptc-description").Change("Construction permit");
        cut.Find("#ptc-fee").Change("100");

        var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create"));
        createButton.Click();

        _mediatorMock.Verify(m => m.Send(It.Is<CreatePermitTypeCommand>(c =>
            c.Name == "Building Permit" && c.Fee == 100m), default), Times.Once);
        Assert.EndsWith($"/admin/permit-types/{newId}/designer", nav.Uri);
    }

    [Fact]
    public void Should_ShowError_WhenCreateFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePermitTypeCommand>(), default))
            .ThrowsAsync(new FluentValidation.ValidationException("Name is required"));

        var cut = Render<PermitTypeCreate>();

        cut.Find("#ptc-name").Change("");
        var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create"));
        createButton.Click();

        Assert.Contains("Unable to create", cut.Markup);
    }

    [Fact]
    public void Should_NavigateToList_OnCancel()
    {
        var cut = Render<PermitTypeCreate>();
        var nav = Services.GetRequiredService<NavigationManager>();

        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        Assert.EndsWith("/admin/permit-types", nav.Uri);
    }
}
