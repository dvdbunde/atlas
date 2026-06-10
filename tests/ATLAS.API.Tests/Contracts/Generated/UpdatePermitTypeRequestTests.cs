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
                Fee = 150.00m,
                IsActive = false
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.PermitTypeId);
            Assert.Equal("Updated Permit", request.Name);
            Assert.Equal("Updated description", request.Description);
            Assert.Equal(150.00m, request.Fee);
            Assert.False(request.IsActive);
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
            Assert.Equal(0m, request.Fee);
            Assert.Equal(default(bool), request.IsActive);
        }
    }
}
