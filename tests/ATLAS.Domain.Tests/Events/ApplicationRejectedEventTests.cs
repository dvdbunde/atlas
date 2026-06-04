using System;
using ATLAS.Domain.Events;
using Xunit;

namespace ATLAS.Domain.Tests.Events
{
    public class ApplicationRejectedEventTests
    {
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();
        private readonly string _reasonCode = "IncompleteApplication";

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var @event = new ApplicationRejectedEvent(_applicationId, _officerId, _reasonCode);

            // Assert
            Assert.Equal(_applicationId, @event.ApplicationId);
            Assert.Equal(_officerId, @event.OfficerId);
            Assert.Equal(_reasonCode, @event.ReasonCode);
            Assert.True(@event.Timestamp <= DateTime.UtcNow);
        }
    }
}
