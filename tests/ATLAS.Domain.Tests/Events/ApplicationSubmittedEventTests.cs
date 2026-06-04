using System;
using ATLAS.Domain.Events;
using Xunit;

namespace ATLAS.Domain.Tests.Events
{
    public class ApplicationSubmittedEventTests
    {
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _citizenId = Guid.NewGuid();
        private readonly Guid _permitTypeId = Guid.NewGuid();

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var @event = new ApplicationSubmittedEvent(_applicationId, _citizenId, _permitTypeId);

            // Assert
            Assert.Equal(_applicationId, @event.ApplicationId);
            Assert.Equal(_citizenId, @event.CitizenId);
            Assert.Equal(_permitTypeId, @event.PermitTypeId);
            Assert.True(@event.Timestamp <= DateTime.UtcNow);
            Assert.True(@event.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        }
    }
}
