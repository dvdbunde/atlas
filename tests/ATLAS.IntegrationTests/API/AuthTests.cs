using System.Net;
using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ATLAS.IntegrationTests.API;

public class AuthTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPermitTypes_WithoutAuth_ShouldReturn401()
    {
        var response = await _client.GetAnonymousAsync("/api/permittypes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPermitTypes_AsCitizen_ShouldReturn200()
    {
        var response = await _client.GetAsAsync("/api/permittypes", TestUserBuilder.AsCitizen());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPermitTypes_AsAdmin_ShouldReturn200()
    {
        var response = await _client.GetAsAsync("/api/permittypes", TestUserBuilder.AsAdmin());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetApplications_AsCitizen_ShouldReturn200()
    {
        var response = await _client.GetAsAsync("/api/applications", TestUserBuilder.AsCitizen());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveApplication_AsCitizen_ShouldReturn403()
    {
        var applicationId = TestData.Application1Id;
        var request = new { comments = "Should not be allowed" };
        var response = await _client.PostAsAsync(
            $"/api/applications/{applicationId}/approve",
            request,
            TestUserBuilder.AsCitizen());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_AsOfficer_ShouldReturn403()
    {
        var response = await _client.GetAsAsync("/api/auditlogs", TestUserBuilder.AsOfficer());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ShouldReturn200()
    {
        var response = await _client.GetAsAsync("/api/users", TestUserBuilder.AsAdmin());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }  
}
