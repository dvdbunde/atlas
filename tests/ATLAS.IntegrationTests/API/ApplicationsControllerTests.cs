using ATLAS.API.Contracts.Generated;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;
using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace ATLAS.IntegrationTests.API
{
    [Collection("Sequential Integration Tests")]
    public class ApplicationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;

        public ApplicationsControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _factory.ResetDatabase();
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
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/approve", request, TestUserBuilder.AsOfficer());
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
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsOfficer());            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var request = new RequestInfoRequest {ApplicationId = applicationId, Message = "Please provide additional information" };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/request-info", request, TestUserBuilder.AsOfficer());
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AssignApplicationToMe_Should_Return200OK()
        {
            var applicationId = TestData.Application1Id;
            var request = new { applicationId = applicationId };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/assign", request, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AssignApplicationToMe_AsCitizen_Should_Return403()
        {
            var applicationId = TestData.Application1Id;
            var request = new { applicationId = applicationId };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/assign", request, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AssignApplicationToMe_AlreadyAssignedToOther_Should_Return409()
        {
            var applicationId = TestData.Application1Id;
            var request = new { applicationId = applicationId };
            await _client.PostAsAsync($"/api/applications/{applicationId}/assign", request, TestUserBuilder.AsOfficer());
            var secondResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/assign", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        [Fact]
        public async Task ApproveApplication_Should_CreateReview()
        {
            // Arrange
            var applicationId = TestData.Application1Id;
            var request = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "Approved" };
            
            // Act
            var approveResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/approve", request, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsOfficer());            
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.Approve, detail.Reviews.First().Decision);
        }
        
        [Fact]
        public async Task RejectApplication_Should_CreateReview()
        {
            // Arrange
            var applicationId = TestData.Application1Id;
            var request = new RejectApplicationRequest 
            { 
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE_DOCUMENTATION", 
                Comments = "Rejected" 
            };
            
            // Act
            var rejectResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsOfficer());
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.Reject, detail.Reviews.First().Decision);
            Assert.Equal("INCOMPLETE_DOCUMENTATION", detail.Reviews.First().ReasonCode);
        }
        
        [Fact]
        public async Task RequestInfo_Should_CreateReview()
        {
            // Arrange
            var applicationId = TestData.Application1Id;
            var request = new RequestInfoRequest { ApplicationId = applicationId, Message = "Please provide additional information" };
            
            // Act
            var infoResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/request-info", request, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsOfficer());
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.RequestInfo, detail.Reviews.First().Decision);
        }

        [Fact]
        public async Task FullReviewChain_SubmitThenApprove_ShouldCreateExactlyOneReview()
        {
            // Arrange — create a draft, then treat it like app1 (submitted + under review)
            var appId = TestData.Application1Id; // Already Submitted + UnderReview in seed
        
            // Act — approve
            var approveRequest = new ApproveApplicationRequest { ApplicationId = appId, Comments = "Looks good" };
            var approveResponse = await _client.PostAsAsync($"/api/applications/{appId}/approve", approveRequest, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{appId}", TestUserBuilder.AsOfficer());
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert — exactly 1 Review (from Approve), 0 Reviews before
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.Approve, detail.Reviews.First().Decision);
        }

        [Fact] 
        public async Task ApproveApplication_AsCitizen_ShouldReturn403()
        { 
            var req = new ApproveApplicationRequest{ApplicationId=TestData.Application1Id, Comments="x"};
            var r = await _client.PostAsAsync($"/api/applications/{TestData.Application1Id}/approve", req, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); 
        }

        [Fact]
        public async Task ApproveApplication_AsWrongOfficer_ShouldReturn409()
        {
            var applicationId = TestData.Application1Id;
            // application1 is seeded assigned to the Officer user; act as a DIFFERENT officer
            var otherOfficer = TestUserBuilder.AsUser(
                Guid.NewGuid(), "Other", "other.officer@atlas.test", "Officer");
            var request = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "x" };

            var response = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/approve", request, otherOfficer);

            // Domain guard "Officer can only act on applications assigned to them" -> 409
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        
        [Fact]
        public async Task ApproveApplication_AlreadyApproved_ShouldReturnConflictOr400()
        {
            var applicationId = TestData.Application1Id;
            var firstRequest = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "First approval" };

            // First approve succeeds (200)
            var firstResponse = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/approve", firstRequest, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            // Second approve hits the domain guard (status no longer UnderReview)
            // -> 400 via GlobalExceptionMiddleware (message does not contain "assigned")
            var secondRequest = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "Second approval" };
            var secondResponse = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/approve", secondRequest, TestUserBuilder.AsOfficer());
            Assert.Contains(secondResponse.StatusCode,
                new[] { HttpStatusCode.Conflict, HttpStatusCode.BadRequest });
        }

        [Fact]
        public async Task RejectApplication_AsWrongOfficer_ShouldReturn409()
        {
            var applicationId = TestData.Application1Id;
            var otherOfficer = TestUserBuilder.AsUser(
                Guid.NewGuid(), "Other", "other.officer@atlas.test", "Officer");
            var request = new RejectApplicationRequest
            {
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE",
                Comments = "x"
            };

            var response = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/reject", request, otherOfficer);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_AsWrongOfficer_ShouldReturn409()
        {
            var applicationId = TestData.Application1Id;
            var otherOfficer = TestUserBuilder.AsUser(
                Guid.NewGuid(), "Other", "other.officer@atlas.test", "Officer");
            var request = new RequestInfoRequest
            {
                ApplicationId = applicationId,
                Message = "need more"
            };

            var response = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/request-info", request, otherOfficer);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task RejectApplication_WithEmptyReasonCode_ShouldReturn400()
        {
            var applicationId = TestData.Application1Id;
            var request = new RejectApplicationRequest
            {
                ApplicationId = applicationId,
                ReasonCode = "",
                Comments = "x"
            };

            var response = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsOfficer());            

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApproveApplication_WithEmptyApplicationId_ShouldReturn400()
        {
            // Route requires a GUID, so exercise the validator path via an invalid body instead:
            // send a reject with missing ApplicationId-equivalent (empty GUID) to confirm 400.
            var request = new ApproveApplicationRequest
            {
                ApplicationId = Guid.Empty, // invalid per ApproveApplicationCommandValidator
                Comments = "x"
            };

            var response = await _client.PostAsAsync(
                $"/api/applications/{Guid.Empty}/approve", request, TestUserBuilder.AsOfficer());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RejectApplication_AsCitizen_ShouldReturn403()
        {
            var request = new RejectApplicationRequest
            {
                ApplicationId = TestData.Application1Id,
                ReasonCode = "INCOMPLETE",
                Comments = "x"
            };
            var response = await _client.PostAsAsync(
                $"/api/applications/{TestData.Application1Id}/reject", request, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_AsCitizen_ShouldReturn403()
        {
            var request = new RequestInfoRequest
            {
                ApplicationId = TestData.Application1Id,
                Message = "need more"
            };
            var response = await _client.PostAsAsync(
                $"/api/applications/{TestData.Application1Id}/request-info", request, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResubmitApplication_AsCitizen_ShouldReturn200()
        {
            // Application1 is UnderReview; need an InfoRequested app to test resubmit.
            // First request info as officer, then resubmit as citizen.
            var infoRequest = new RequestInfoRequest { ApplicationId = TestData.Application1Id, Message = "Need more info" };
            await _client.PostAsAsync($"/api/applications/{TestData.Application1Id}/request-info", infoRequest, TestUserBuilder.AsOfficer());

            var response = await _client.PostAsAsync(
                $"/api/applications/{TestData.Application1Id}/resubmit", null, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResubmitApplication_AsOfficer_ShouldReturn403()
        {
            var response = await _client.PostAsAsync(
                $"/api/applications/{TestData.Application1Id}/resubmit", null, TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResubmitApplication_AsWrongCitizen_ShouldReturn403()
        {
            // Application1 is owned by the seeded citizen; act as a DIFFERENT citizen
            var otherCitizen = TestUserBuilder.AsUser(
                Guid.NewGuid(), "Other", "other.citizen@atlas.test", "Citizen");
            var response = await _client.PostAsAsync(
                $"/api/applications/{TestData.Application1Id}/resubmit", null, otherCitizen);

            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");                

            // Handler throws UnauthorizedAccessException → 401, or middleware → 403
            Assert.Contains(response.StatusCode,
                new[] { HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized });
        }

        [Fact]
        public async Task GetApplicationActivity_Should_Return200OK()
        {
            var response = await _client.GetAsAsync(
                $"/api/applications/{TestData.Application1Id}/activity", TestUserBuilder.AsOfficer());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationActivity_Should_Return401_ForAnonymous()
        {
            var response = await _client.GetAnonymousAsync(
                $"/api/applications/{TestData.Application1Id}/activity");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationActivity_WithUnknownApp_ShouldReturn200_Empty()
        {
            var response = await _client.GetAsAsync(
                $"/api/applications/{Guid.NewGuid()}/activity", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("[]", content); // empty array
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
