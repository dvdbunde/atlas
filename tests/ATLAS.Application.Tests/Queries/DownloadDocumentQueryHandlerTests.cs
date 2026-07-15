using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Documents;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class DownloadDocumentQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMediator> _mockMediator;
        private readonly DownloadDocumentQueryHandler _handler;
        private readonly Guid _testUserId;

        public DownloadDocumentQueryHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMediator = new Mock<IMediator>();
            _testUserId = Guid.NewGuid();

            _mockCurrentUser.Setup(s => s.UserId).Returns(_testUserId);

            _handler = new DownloadDocumentQueryHandler(
                _mockRepository.Object,
                _mockFileStorage.Object,
                _mockCurrentUser.Object,
                _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSasUri_WhenAuthorized()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(_testUserId, Guid.NewGuid(), "Test");
            // Use reflection to set Id
            var idProp = typeof(ATLAS.Domain.Entities.Application).GetProperty("Id");
            idProp?.SetValue(application, applicationId);

            // Add document
            var doc = application.AddDocument(documentId, "ParkingPermit", "test.pdf", "application/pdf", 1024, "https://blob.url/test.pdf", _testUserId);

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockFileStorage.Setup(s => s.GenerateDownloadSasUriAsync(
                    It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://blob.url/test.pdf?sig=abc&se=2026-01-01&sp=r");

            var query = new DownloadDocumentQuery { DocumentId = documentId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.pdf", result!.FileName);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Contains("sig=", result.SasUri);
            _mockMediator.Verify(m => m.Publish(
                It.Is<DocumentDownloadedEvent>(e => e.DocumentId == documentId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenDocumentNotFound()
        {
            _mockRepository.Setup(r => r.GetByDocumentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application)null);

            var result = await _handler.Handle(new DownloadDocumentQuery { DocumentId = Guid.NewGuid() }, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenOwnershipMismatch()
        {
            var documentId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(otherUserId, Guid.NewGuid(), "Test");
            var idProp = typeof(ATLAS.Domain.Entities.Application).GetProperty("Id");
            idProp?.SetValue(application, Guid.NewGuid());
            application.AddDocument(documentId, "ParkingPermit", "test.pdf", "application/pdf", 1024, "https://blob.url", otherUserId);

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(new DownloadDocumentQuery { DocumentId = documentId }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldUseOneHourExpiry()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(_testUserId, Guid.NewGuid(), "Test");
            application.AddDocument(documentId, "ParkingPermit", "test.pdf", "application/pdf", 1024, "https://blob.url/test.pdf", _testUserId);

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            TimeSpan? capturedExpiry = null;
            _mockFileStorage.Setup(s => s.GenerateDownloadSasUriAsync(
                    It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback<string, TimeSpan, CancellationToken>((_, expiry, _) => capturedExpiry = expiry)
                .ReturnsAsync("https://blob.url/test.pdf?sig=abc");

            await _handler.Handle(new DownloadDocumentQuery { DocumentId = documentId }, CancellationToken.None);

            Assert.NotNull(capturedExpiry);
            Assert.Equal(1, capturedExpiry!.Value.TotalHours);
        }

        [Fact]
        public async Task Handle_ShouldAllowOfficer_ForAnotherCitizensApplication()
        {
            // Arrange — document belongs to a citizen, requested by an Officer
            var documentId = Guid.NewGuid();
            var citizenUserId = Guid.NewGuid();
            var officerUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(citizenUserId, Guid.NewGuid(), "Test");
            var idProp = typeof(ATLAS.Domain.Entities.Application).GetProperty("Id");
            idProp?.SetValue(application, Guid.NewGuid());
            application.AddDocument(documentId, "SitePlan", "plan.pdf", "application/pdf", 1024, "https://blob.url/secret", citizenUserId);

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockCurrentUser.Setup(s => s.UserId).Returns(officerUserId);
            _mockCurrentUser.Setup(s => s.Role).Returns("Officer");
            _mockFileStorage.Setup(s => s.GenerateDownloadSasUriAsync(
                    It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://blob.url/secret?sig=abc");

            // Act
            var result = await _handler.Handle(new DownloadDocumentQuery { DocumentId = documentId }, CancellationToken.None);

            // Assert — Officer can review a citizen's document (M1)
            Assert.NotNull(result);
            Assert.Equal("plan.pdf", result!.FileName);
        }

        [Fact]
        public async Task Handle_ShouldDenyCitizen_ForAnotherCitizensApplication()
        {
            // Arrange — document belongs to citizen A, requested by citizen B
            var documentId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var otherCitizenUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(ownerUserId, Guid.NewGuid(), "Test");
            var idProp = typeof(ATLAS.Domain.Entities.Application).GetProperty("Id");
            idProp?.SetValue(application, Guid.NewGuid());
            application.AddDocument(documentId, "SitePlan", "plan.pdf", "application/pdf", 1024, "https://blob.url/secret", ownerUserId);

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockCurrentUser.Setup(s => s.UserId).Returns(otherCitizenUserId);
            _mockCurrentUser.Setup(s => s.Role).Returns("Citizen");

            // Act / Assert — ownership preserved
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(new DownloadDocumentQuery { DocumentId = documentId }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldDeny_WhenUnauthenticated()
        {
            var documentId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test");
            application.AddDocument(documentId, "SitePlan", "plan.pdf", "application/pdf", 1024, "https://blob.url/secret", Guid.NewGuid());

            _mockRepository.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockCurrentUser.Setup(s => s.UserId).Returns((Guid?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(new DownloadDocumentQuery { DocumentId = documentId }, CancellationToken.None));
        }
    }
}