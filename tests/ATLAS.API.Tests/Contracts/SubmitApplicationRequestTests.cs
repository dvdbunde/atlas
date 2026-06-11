using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class SubmitApplicationRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var notes = "Test application";

            // Act
            var request = new SubmitApplicationRequest
            {
                PermitTypeId = permitTypeId,
                CitizenNotes = notes
            };

            // Assert
            Assert.Equal(permitTypeId, request.PermitTypeId);
            Assert.Equal(notes, request.CitizenNotes);
        }      
    }
}
