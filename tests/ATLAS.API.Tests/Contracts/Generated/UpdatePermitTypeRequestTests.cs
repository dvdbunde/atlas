using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class UpdatePermitTypeRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new UpdatePermitTypeRequest
            {
                PermitTypeId = Guid.NewGuid(),
                Name = "Updated Permit",
                Description = "Updated description",
                IsActive = false,
                EstimatedProcessingDays = 5,
                DeactivatedByAdminId = Guid.NewGuid()
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.PermitTypeId);
            Assert.Equal("Updated Permit", request.Name);
            Assert.Equal("Updated description", request.Description);
            Assert.False(request.IsActive);
            Assert.Equal(5, request.EstimatedProcessingDays);
            Assert.NotEqual(Guid.Empty, request.DeactivatedByAdminId);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new UpdatePermitTypeRequest();

            // Assert
            Assert.Equal(default(Guid), request.PermitTypeId);
            Assert.Null(request.Name);
            Assert.Null(request.Description);
            Assert.Equal(default(bool), request.IsActive);
            Assert.Equal(default(int), request.EstimatedProcessingDays);
            Assert.Null(request.DeactivatedByAdminId);
        }
    }
}
