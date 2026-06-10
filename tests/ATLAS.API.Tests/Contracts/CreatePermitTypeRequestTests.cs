using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class CreatePermitTypeRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var name = "Building Permit";
            var description = "Permit for building construction";
            var fee = 150.00m;            

            // Act
            var request = new CreatePermitTypeRequest
            {
                Name = name,
                Description = description,
                Fee = fee                
            };

            // Assert
            Assert.Equal(name, request.Name);
            Assert.Equal(description, request.Description);
            Assert.Equal(fee, request.Fee);            
        }

        [Fact]
        public void Properties_WithNullDescription_ShouldSetNullDescription()
        {
            // Arrange & Act
            var request = new CreatePermitTypeRequest
            {
                Name = "Test",
                Description = null,
                Fee = 100m                
            };

            // Assert
            Assert.Null(request.Description);
        }  
    }
}
