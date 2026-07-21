using System.Security.Claims;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using ATLAS.Blazor.Components.Shared.Admin;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypeDesignerTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public PermitTypeDesignerTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth();
    }

    private static PermitTypeDto SampleDto(Guid id) => new()
    {
        Id = id,
        Name = "Building Permit",
        Description = "Construction permit",
        Fee = 100m,
        IsActive = true
    };

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
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var id = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).Returns(tcs.Task);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderGeneralSectionByDefault_WhenLoaded()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find("#ptd-name"));
        Assert.NotNull(cut.Find("#ptd-description"));
        Assert.Contains("General", cut.Markup);
    }

    [Fact]
    public void Should_RenderPlaceholderSections_WhenNavigated()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        var fieldsTab = cut.FindAll("button").First(b => b.TextContent.Contains("Fields"));
        fieldsTab.Click();
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("Fields", cut.Markup);

        var docsTab = cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements"));
        docsTab.Click();
        Assert.Contains("Document Requirements", cut.Markup);

        var previewTab = cut.FindAll("button").First(b => b.TextContent.Contains("Preview"));
        previewTab.Click();
        Assert.Contains("Live Preview", cut.Markup);
    }

    [Fact]
    public void Should_ShowNotFound_WhenDtoIsNull()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync((PermitTypeDto?)null);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.Contains("Permit type not found", cut.Markup);
    }

    [Fact]
    public void Should_SendUpdateCommand_OnSave_WhenDirty()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePermitTypeGeneralInformationCommand>(), default))
            .ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        var nameInput = cut.Find("#ptd-name");
        nameInput.Input("Renovation Permit");

        Assert.Contains("You have unsaved changes", cut.Markup);

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
        saveButton.Click();

        _mediatorMock.Verify(m => m.Send(It.Is<UpdatePermitTypeGeneralInformationCommand>(c =>
            c.PermitTypeId == id && c.Name == "Renovation Permit"), default), Times.Once);
    }

    [Fact]
    public void Should_NavigateToDetail_OnCancel_WhenDirty()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        var nav = Services.GetRequiredService<NavigationManager>();

        cut.Find("#ptd-name").Input("Renovation Permit");
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        Assert.EndsWith($"/admin/permit-types/{id}", nav.Uri);
    }

    [Fact]
    public void Should_RegisterUnsavedChanges_OnInput()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.Find("#ptd-name").Input("Changed Name");

        Assert.Contains("You have unsaved changes", cut.Markup);
    }
}
