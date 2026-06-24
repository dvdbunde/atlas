using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Commands;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.ValueObjects;
using MediatR;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class UploadDocumentCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly UploadDocumentCommandHandler _handler;
        private readonly Guid _testUserId;

        public UploadDocumentCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockMediator = new Mock<IMediator>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _testUserId = Guid.NewGuid();

            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);

            _handler = new UploadDocumentCommandHandler(
                _mockRepository.Object,
                _mockPermitTypeRepository.Object,
                _mockFileStorageService.Object,
                _mockMediator.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ShouldUploadAndPersist_WhenOwnershipValid()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var permitTypeId = Guid.NewGuid();
            var citizenId = _testUserId; // match current user

            var application = new ATLAS.Domain.Entities.Application(citizenId, permitTypeId, "Test notes");
            application.ClearDomainEvents();

            // Use reflection to set private Id
            var idField = typeof(ATLAS.Domain.Entities.Application)
                .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(application, applicationId);

            var permitType = new PermitType("Test Permit", "Description", 100m);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);
            _mockFileStorageService.Setup(s => s.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileUploadResult("https://blob.url/doc.pdf", 1024));

            var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = content,
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockFileStorageService.Verify(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.Is<string>(p => p.Contains(applicationId.ToString())), // ADR-015 naming
                "application/pdf",
                It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(
                It.Is<DocumentUploadedEvent>(e => e.ApplicationId == applicationId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenApplicationNotFound()
        {
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application)null);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = new MemoryStream(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024
            };

            var result = await _handler.Handle(command, CancellationToken.None);
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenOwnershipMismatch()
        {
            var applicationId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(otherUserId, Guid.NewGuid(), "Test");
            var idField = typeof(ATLAS.Domain.Entities.Application)
                .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(application, applicationId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = new MemoryStream(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _handler.Handle(null, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldUseAdrBlobNaming()
        {
            // Verify ADR-015 naming: {applicationId}/{documentId}/{fileName}
            var applicationId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(_testUserId, Guid.NewGuid(), "Test");
            application.ClearDomainEvents();
            var idField = typeof(ATLAS.Domain.Entities.Application)
                .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(application, applicationId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PermitType("Test", "Desc", 100m));
            _mockFileStorageService.Setup(s => s.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileUploadResult("https://blob.url/doc.pdf", 1024));

            string? capturedBlobPath = null;
            _mockFileStorageService.Setup(s => s.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, string, string, CancellationToken>((_, path, _, _) => capturedBlobPath = path)
                .ReturnsAsync(new FileUploadResult("https://blob.url/doc.pdf", 1024));

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test")),
                FileName = "site-plan.pdf",
                ContentType = "application/pdf",
                FileSize = 512
            };

            await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(capturedBlobPath);
            Assert.StartsWith(applicationId.ToString(), capturedBlobPath!);
            Assert.EndsWith("site-plan.pdf", capturedBlobPath!);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenFileTypeNotInDocumentRequirements()
        {
            var permitType = new PermitType("Test", "Desc", 100m);
            permitType.AddDocumentRequirement("Site Plan", true, new[] { ".pdf", ".png" }, 10 * 1024 * 1024);

            var applicationId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Test");
            application.ClearDomainEvents();
            var idField = typeof(ATLAS.Domain.Entities.Application)
                .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(application, applicationId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test")),
                FileName = "document.exe",
                ContentType = "application/x-msdownload",
                FileSize = 512
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenFileExceedsDocumentRequirementSize()
        {
            var permitType = new PermitType("Test", "Desc", 100m);
            permitType.AddDocumentRequirement("Site Plan", true, new[] { ".pdf" }, 1000); // 1KB max

            var applicationId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Test");
            application.ClearDomainEvents();
            var idField = typeof(ATLAS.Domain.Entities.Application)
                .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(application, applicationId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test")),
                FileName = "doc.pdf",
                ContentType = "application/pdf",
                FileSize = 2000 // exceeds 1000
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }
}