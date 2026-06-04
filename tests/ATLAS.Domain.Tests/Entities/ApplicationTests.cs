using System;
using Xunit;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Domain.Tests.Entities
{
    public class ApplicationTests
    {
        private readonly Guid _citizenId = Guid.NewGuid();
        private readonly Guid _permitTypeId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        [Fact]
        public void Create_ShouldInitializeWithDraftStatus()
        {
            // Arrange & Act
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Assert
            Assert.Equal(_citizenId, application.CitizenId);
            Assert.Equal(_permitTypeId, application.PermitTypeId);
            Assert.Equal("Test notes", application.CitizenNotes);
            Assert.Equal(ApplicationStatus.Draft, application.Status);
            Assert.Null(application.SubmittedDate);
            Assert.Null(application.ReviewedDate);
        }

        [Fact]
        public void Submit_ShouldTransitionFromDraftToSubmitted()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act
            application.Submit();

            // Assert
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
            Assert.NotNull(application.SubmittedDate);
            Assert.True(application.SubmittedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Submit_ShouldThrowException_WhenNotInDraftStatus()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit(); // Now it's Submitted

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => application.Submit());
            Assert.Equal("Only draft applications can be submitted", exception.Message);
        }
    }
}
