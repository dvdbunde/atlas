using ATLAS.API.Contracts.Generated;
using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace ATLAS.IntegrationTests.API
{
    public class ApplicationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        private readonly ITestOutputHelper _output;

        public ApplicationsControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }
        
        [Fact]
        public async Task GetApplications_Should_Return200OK()
        {
            var response = await _client.GetAsAsync("/api/applications", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetApplications_WithCitizenId_Should_Return200OK()
        {
            var citizenId = TestData.CitizenUserId;
            var response = await _client.GetAsAsync($"/api/applications?citizenId={citizenId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SubmitApplication_Should_Return201Created()
        {
            var request = new
            {
                permitTypeId = TestData.BuildingPermitTypeId,
                citizenNotes = "Test application"
            };
            var response = await _client.PostAsAsync("/api/applications", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationById_WithValidId_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationById_WithInvalidId_Should_Return404NotFound()
        {
            var applicationId = Guid.NewGuid();
            var response = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApproveApplication_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var request = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "Approved" };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/approve", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RejectApplication_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var request = new RejectApplicationRequest 
            { 
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE_DOCUMENTATION", 
                Comments = "Rejected" 
            };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var request = new RequestInfoRequest {ApplicationId = applicationId, Message = "Please provide additional information" };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/request-info", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AssignToOfficer_Should_Return200OK()
        {
            var applicationId = TestData.Application2Id;
            var request = new { officerId = TestData.OfficerUserId };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/assign", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
