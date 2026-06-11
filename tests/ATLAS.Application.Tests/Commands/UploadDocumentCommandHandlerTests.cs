using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Commands;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class UploadDocumentCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly UploadDocumentCommandHandler _handler;
        private readonly Guid _testUserId;

        public UploadDocumentCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _testUserId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new UploadDocumentCommandHandler(
                _mockRepository.Object,
                _mockMediator.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrue_WhenApplicationExists()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                BlobUrl = "https://blob.com/test.pdf"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<DocumentUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenApplicationNotFound()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application)null);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                BlobUrl = "https://blob.com/test.pdf"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _handler.Handle(null, CancellationToken.None));
        }
    }
}
