using System.Linq;
using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.Components.Pages.Admin;
using Bunit;
using Bunit.TestDoubles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

/// <summary>
/// Verifies the Administration Portal authorization boundary. The Admin pages are
/// protected with [Authorize(Roles = "Admin")] and must be denied to Officers,
/// Citizens, and anonymous users while allowed for Administrators.
///
/// Note: bUnit does not enforce Blazor authorization policies at render time, so the
/// boundary is verified by asserting the [Authorize] attribute is declared on each
/// page (the framework-enforced gate) and that the dashboard renders correctly for an
/// authenticated Administrator with the required role.
/// </summary>
public class AdminAuthorizationTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public AdminAuthorizationTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    [Fact]
    public void AdminDashboard_ShouldDeclareAdminRoleAuthorization()
    {
        var attribute = typeof(AdminDashboard)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }

    [Theory]
    [InlineData(typeof(PermitTypes))]
    [InlineData(typeof(DynamicForms))]
    [InlineData(typeof(EmailTemplates))]
    [InlineData(typeof(ReferenceData))]
    [InlineData(typeof(Officers))]
    [InlineData(typeof(SystemSettings))]
    [InlineData(typeof(Users))]
    [InlineData(typeof(UserDetail))]
    public void AdminPlaceholderPages_ShouldDeclareAdminRoleAuthorization(System.Type pageType)
    {
        var attribute = pageType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }

    [Fact]
    public void AdminDashboard_ShouldRender_ForAuthenticatedAdministrator()
    {
        var authContext = AddAuthorization();
        authContext.SetAuthorized("admin@atlas.test", AuthorizationState.Authorized);
        authContext.SetRoles(new[] { "Admin" });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(new AdminDashboardDto
            {
                PermitTypeCount = 4,
                ApplicationCount = 12,
                OfficerCount = 3,
                ActiveEmailTemplateCount = 0
            });

        var cut = Render<AdminDashboard>();

        Assert.NotNull(cut.Find("h1"));
        Assert.Contains("Administration Dashboard", cut.Markup);
    }

    [Theory]
    [InlineData(typeof(PermitTypes))]
    [InlineData(typeof(PermitTypeDetail))]
    [InlineData(typeof(PermitTypeSettings))]
    [InlineData(typeof(PermitTypeDesigner))]
    public void PermitTypeAdminPages_ShouldDeclareAdminRoleAuthorization(System.Type pageType)
    {
        var attribute = pageType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }
}

