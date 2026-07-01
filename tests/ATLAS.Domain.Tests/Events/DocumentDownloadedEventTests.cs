using System;
using ATLAS.Domain.Events;
using Xunit;

namespace ATLAS.Domain.Tests.Events
{
    public class DocumentDownloadedEventTests
    {
        private readonly Guid _documentId = Guid.NewGuid();
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _downloadedById = Guid.NewGuid();
        private const string BlobUrl = "https://storage.blob.core.windows.net/permit-documents/app-id/doc-id/file.pdf";

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            var @event = new DocumentDownloadedEvent(_documentId, _applicationId, _downloadedById, BlobUrl);

            // Assert
            Assert.Equal(_documentId, @event.DocumentId);
            Assert.Equal(_applicationId, @event.ApplicationId);
            Assert.Equal(_downloadedById, @event.DownloadedById);
            Assert.Equal(BlobUrl, @event.BlobUrl);
            Assert.True(@event.Timestamp <= DateTime.UtcNow);
            Assert.True(@event.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void Constructor_ShouldSetTimestampToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var @event = new DocumentDownloadedEvent(_documentId, _applicationId, _downloadedById, BlobUrl);

            // Assert
            Assert.InRange(@event.Timestamp, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        }
    }
}