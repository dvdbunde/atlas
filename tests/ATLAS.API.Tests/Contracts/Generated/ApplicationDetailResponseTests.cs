using ATLAS.API.Contracts.Generated;
using System.Collections.ObjectModel;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class ApplicationDetailResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new ApplicationDetailResponse
            {
                Id = Guid.NewGuid(),
                ApplicationNumber = "APP-2024-001",
                Status = 2,
                SubmittedDate = DateTimeOffset.UtcNow,
                CitizenId = Guid.NewGuid(),
                PermitTypeId = Guid.NewGuid(),
                CitizenName = "John Doe",
                PermitTypeName = "Building Permit",
                ReviewedDate = DateTimeOffset.UtcNow,
                CitizenNotes = "Need permit urgently",
                OfficerNotes = "Approved after review",
                OfficerName = "Jane Smith"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("APP-2024-001", response.ApplicationNumber);
            Assert.Equal(2, response.Status);
            Assert.NotNull(response.ReviewedDate);
            Assert.Equal("Need permit urgently", response.CitizenNotes);
            Assert.Equal("Approved after review", response.OfficerNotes);
            Assert.Equal("Jane Smith", response.OfficerName);
        }

        [Fact]
        public void Documents_ShouldBeInitializedToEmptyCollection()
        {
            // Arrange & Act
            var response = new ApplicationDetailResponse();

            // Assert
            Assert.NotNull(response.Documents);
            Assert.Empty(response.Documents);
        }

        [Fact]
        public void Reviews_ShouldBeInitializedToEmptyCollection()
        {
            // Arrange & Act
            var response = new ApplicationDetailResponse();

            // Assert
            Assert.NotNull(response.Reviews);
            Assert.Empty(response.Reviews);
        }

        [Fact]
        public void ShouldInheritFromApplicationSummaryResponse()
        {
            // Arrange & Act
            var response = new ApplicationDetailResponse
            {
                Id = Guid.NewGuid(),
                ApplicationNumber = "APP-2024-001"
            };

            // Assert
            Assert.Equal(response.Id, ((ApplicationSummaryResponse)response).Id);
            Assert.Equal(response.ApplicationNumber, ((ApplicationSummaryResponse)response).ApplicationNumber);
        }
    }
}
