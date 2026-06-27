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
        public async Task SubmitApplication_Should_Return201Created()
        {
            var request = new
            {
                permitTypeId = TestData.BuildingPermitTypeId,
                citizenNotes = "Test application"
            };
            var response = await _client.PostAsAsync("/api/applications", request, TestUserBuilder.AsAdmin());
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
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
            var applicationId = TestData.Application5Id;
            var request = new RejectApplicationRequest 
            { 
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE_DOCUMENTATION", 
                Comments = "Rejected" 
            };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsAdmin());
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_Should_Return200OK()
        {
            var applicationId = TestData.Application6Id;
            var request = new RequestInfoRequest {ApplicationId = applicationId, Message = "Please provide additional information" };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/request-info", request, TestUserBuilder.AsAdmin());
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
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

        [Fact]
        public async Task ApproveApplication_Should_CreateReview()
        {
            // Arrange
            var applicationId = TestData.Application1Id;
            var request = new ApproveApplicationRequest { ApplicationId = applicationId, Comments = "Approved" };
            
            // Act
            var approveResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/approve", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsAdmin());            
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
            var applicationId = TestData.Application5Id;
            var request = new RejectApplicationRequest 
            { 
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE_DOCUMENTATION", 
                Comments = "Rejected" 
            };
            
            // Act
            var rejectResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/reject", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsAdmin());
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
            var applicationId = TestData.Application6Id;
            var request = new RequestInfoRequest { ApplicationId = applicationId, Message = "Please provide additional information" };
            
            // Act
            var infoResponse = await _client.PostAsAsync($"/api/applications/{applicationId}/request-info", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{applicationId}", TestUserBuilder.AsAdmin());
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.RequestInfo, detail.Reviews.First().Decision);
        }

        [Fact]
        public async Task FullReviewChain_SubmitThenApprove_ShouldCreateExactlyOneReview()
        {
            // Arrange — create a draft, then treat it like app5 (submitted + under review)
            var appId = TestData.Application5Id; // Already Submitted + UnderReview in seed
        
            // Act — approve
            var approveRequest = new ApproveApplicationRequest { ApplicationId = appId, Comments = "Looks good" };
            var approveResponse = await _client.PostAsAsync($"/api/applications/{appId}/approve", approveRequest, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        
            var detailResponse = await _client.GetAsAsync($"/api/applications/{appId}", TestUserBuilder.AsAdmin());
            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
        
            // Assert — exactly 1 Review (from Approve), 0 Reviews before
            Assert.NotNull(detail);
            Assert.Single(detail.Reviews);
            Assert.Equal(ReviewResponseDecision.Approve, detail.Reviews.First().Decision);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
