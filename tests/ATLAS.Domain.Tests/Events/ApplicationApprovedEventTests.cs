using System;
using ATLAS.Domain.Events;
using Xunit;

namespace ATLAS.Domain.Tests.Events
{
    public class ApplicationApprovedEventTests
    {
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var @event = new ApplicationApprovedEvent(_applicationId, _officerId);

            // Assert
            Assert.Equal(_applicationId, @event.ApplicationId);
            Assert.Equal(_officerId, @event.OfficerId);
            Assert.True(@event.Timestamp <= DateTime.UtcNow);
            Assert.True(@event.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        }
    }
}
