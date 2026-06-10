using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class UpdatePermitTypeRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();            
            var name = "Updated Permit";
            var description = "Updated description";            
            var isActive = false;
            var fee = 150.00m;

            // Act
            var request = new UpdatePermitTypeRequest
            {
                PermitTypeId = permitTypeId,
                Name = name,
                Description = description,
                Fee = fee,
                IsActive = isActive
            };

            // Assert
            Assert.Equal(permitTypeId, request.PermitTypeId);
            Assert.Equal(name, request.Name);
            Assert.Equal(description, request.Description);
            Assert.Equal(fee, request.Fee);
            Assert.Equal(isActive, request.IsActive);
        }

        [Fact]
        public void Properties_WithNullDescription_ShouldSetNullDescription()
        {
            // Arrange & Act
            var request = new UpdatePermitTypeRequest
            {
                PermitTypeId = Guid.NewGuid(),
                Name = "Test",
                Description = null,                
                IsActive = true
            };

            // Assert
            Assert.Null(request.Description);
        }
    }
}
