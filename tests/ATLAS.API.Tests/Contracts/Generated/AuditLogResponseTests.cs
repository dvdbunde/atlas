using ATLAS.API.Contracts.Generated;
using System;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class AuditLogResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new AuditLogResponse
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ActionType = "Create",
                Timestamp = DateTimeOffset.UtcNow,
                RecordId = Guid.NewGuid(),
                Details = "Application created"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.NotEqual(Guid.Empty, response.UserId);
            Assert.Equal("Create", response.ActionType);
            Assert.NotNull(response.Timestamp);
            Assert.NotEqual(Guid.Empty, response.RecordId);
            Assert.Equal("Application created", response.Details);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new AuditLogResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.UserId);
            Assert.Null(response.ActionType);
            Assert.Equal(default(DateTimeOffset), response.Timestamp);
            Assert.Equal(default(Guid), response.RecordId);
            Assert.Null(response.Details);
        }
    }
}
