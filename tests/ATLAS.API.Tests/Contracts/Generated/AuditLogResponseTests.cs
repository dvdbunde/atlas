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
                Action = "Create",
                EntityType = "Application",
                EntityId = Guid.NewGuid(),
                Details = "Application created",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = "192.168.1.1"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.NotEqual(Guid.Empty, response.UserId);
            Assert.Equal("Create", response.Action);
            Assert.Equal("Application", response.EntityType);
            Assert.NotEqual(Guid.Empty, response.EntityId);
            Assert.Equal("Application created", response.Details);
            Assert.NotNull(response.Timestamp);
            Assert.Equal("192.168.1.1", response.IpAddress);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new AuditLogResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.UserId);
            Assert.Null(response.Action);
            Assert.Null(response.EntityType);
            Assert.Equal(default(Guid), response.EntityId);
            Assert.Null(response.Details);
            Assert.Equal(default(DateTimeOffset), response.Timestamp);
            Assert.Null(response.IpAddress);
        }
    }
}
