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
            var deactivatedByAdminId = Guid.NewGuid();

            // Act
            var request = new UpdatePermitTypeRequest
            {
                PermitTypeId = permitTypeId,
                Name = name,
                Description = description,                
                IsActive = isActive,
                DeactivatedByAdminId = deactivatedByAdminId,
                EstimatedProcessingDays = 10
            };

            // Assert
            Assert.Equal(permitTypeId, request.PermitTypeId);
            Assert.Equal(name, request.Name);
            Assert.Equal(description, request.Description);            
            Assert.Equal(isActive, request.IsActive);
            Assert.Equal(deactivatedByAdminId, request.DeactivatedByAdminId);
            Assert.Equal(10, request.EstimatedProcessingDays);
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
