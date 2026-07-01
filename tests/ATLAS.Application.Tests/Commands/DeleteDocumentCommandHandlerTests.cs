using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Documents;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class DeleteDocumentCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<ILogger<DeleteDocumentCommandHandler>> _mockLogger;
        private readonly DeleteDocumentCommandHandler _handler;
        private readonly Guid _testUserId;

        public DeleteDocumentCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockLogger = new Mock<ILogger<DeleteDocumentCommandHandler>>();
            _testUserId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new DeleteDocumentCommandHandler(
                _mockRepository.Object,
                _mockCurrentUserService.Object,
                _mockFileStorageService.Object,
                _mockLogger.Object);
        }

        private Domain.Entities.Application CreateDraftWithDocument()
        {
            var citizenId = _testUserId;
            var application = new Domain.Entities.Application(citizenId, Guid.NewGuid(), "Test notes");
            application.AddDocument(
                Guid.NewGuid(),
                "Building Permit",
                "test.pdf",
                "application/pdf",
                1024,
                "https://blob.url/test.pdf",
                citizenId);
            return application;
        }

        [Fact]
        public async Task Handle_ShouldDeleteDocumentAndBlob_WhenValid()
        {
            // Arrange
            var application = CreateDraftWithDocument();
            var documentId = application.Documents.First().Id;
            var blobUrl = application.Documents.First().BlobUrl;

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockFileStorageService.Setup(s => s.DeleteAsync(blobUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = new DeleteDocumentCommand
            {
                ApplicationId = application.Id,
                DocumentId = documentId
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Empty(application.Documents);
            _mockFileStorageService.Verify(s => s.DeleteAsync(blobUrl, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldLogWarning_WhenBlobNotFound()
        {
            // Arrange
            var application = CreateDraftWithDocument();
            var documentId = application.Documents.First().Id;
            var blobUrl = application.Documents.First().BlobUrl;

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockFileStorageService.Setup(s => s.DeleteAsync(blobUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Blob not found

            var command = new DeleteDocumentCommand
            {
                ApplicationId = application.Id,
                DocumentId = documentId
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert — should still remove document record even when blob missing
            Assert.Empty(application.Documents);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenApplicationNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Application?)null);

            var command = new DeleteDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                DocumentId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenOwnershipMismatch()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var application = new Domain.Entities.Application(otherUserId, Guid.NewGuid(), "Test");
            application.AddDocument(Guid.NewGuid(), "Building Permit", "doc.pdf", "application/pdf", 1024, "https://blob.url/doc.pdf", otherUserId);

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new DeleteDocumentCommand
            {
                ApplicationId = application.Id,
                DocumentId = application.Documents.First().Id
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenApplicationNotDraft()
        {
            // Arrange
            var application = new Domain.Entities.Application(_testUserId, Guid.NewGuid(), "Test");
            application.AddDocument(Guid.NewGuid(), "Building Permit", "doc.pdf", "application/pdf", 1024, "https://blob.url/doc.pdf", _testUserId);
            application.Submit();

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new DeleteDocumentCommand
            {
                ApplicationId = application.Id,
                DocumentId = application.Documents.First().Id
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenDocumentNotFound()
        {
            // Arrange
            var application = new Domain.Entities.Application(_testUserId, Guid.NewGuid(), "Test");

            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new DeleteDocumentCommand
            {
                ApplicationId = application.Id,
                DocumentId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRequestIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _handler.Handle(null!, CancellationToken.None));
        }
    }
}