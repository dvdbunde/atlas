using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class SubmitDraftCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockAppRepository;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<SubmitDraftCommandHandler>> _mockLogger;
        private readonly SubmitDraftCommandHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _permitTypeId;
        private readonly ATLAS.Domain.Entities.Application _testApplication;
        private readonly PermitType _testPermitType;

        public SubmitDraftCommandHandlerTests()
        {
            _mockAppRepository = new Mock<IApplicationRepository>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<SubmitDraftCommandHandler>>();
            _testUserId = Guid.NewGuid();
            _permitTypeId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);

            _testPermitType = new PermitType("Building Permit", "Desc", 100m);
            _testPermitType.AddField("PropertyAddress", FieldType.Text, true);
            _testPermitType.AddField("LotSize", FieldType.Number, false);

            _testApplication = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            _testApplication.AddFieldValue("PropertyAddress", "123 Main St", 0);
            _testApplication.AddFieldValue("LotSize", "5000", 1);

            _handler = new SubmitDraftCommandHandler(
                _mockAppRepository.Object,
                _mockPermitTypeRepository.Object,
                _mockCurrentUserService.Object,
                _mockMediator.Object,
                _mockLogger.Object);
        }

        private void SetupFound()
        {
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testApplication);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testPermitType);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldSubmitApplication()
        {
            // Arrange
            SetupFound();
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ApplicationStatus.Submitted, _testApplication.Status);
            Assert.NotNull(_testApplication.SubmittedDate);
            _mockAppRepository.Verify(r => r.UpdateAsync(_testApplication, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldPublishApplicationSubmittedEvent()
        {
            // Arrange
            SetupFound();
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockMediator.Verify(
                m => m.Publish(
                    It.Is<ApplicationSubmittedEvent>(e =>
                        e.ApplicationId == _testApplication.Id &&
                        e.CitizenId == _testUserId &&
                        e.PermitTypeId == _permitTypeId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application?)null);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NotOwnApplication_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(otherUserId, _permitTypeId, "Notes");
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NotDraftStatus_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.Submit();
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_PermitTypeNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testApplication);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PermitType?)null);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InvalidFieldName_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.AddFieldValue("NonExistentField", "value", 0); // not in permit type
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testPermitType);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("not defined", exception.Message);
        }

        [Fact]
        public async Task Handle_RequiredFieldMissing_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.AddFieldValue("LotSize", "5000", 0); // "PropertyAddress" is required but missing
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testPermitType);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Required field", exception.Message);
            Assert.Contains("PropertyAddress", exception.Message);
        }

        [Fact]
        public async Task Handle_RequiredFieldEmptyValue_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.AddFieldValue("PropertyAddress", "   ", 0); // whitespace
            application.AddFieldValue("LotSize", "5000", 1);
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testPermitType);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("must have a value", exception.Message);
        }

        [Fact]
        public async Task Handle_NoFieldValues_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            // No field values added
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testPermitType);
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("at least one field value", exception.Message);
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }

        #region Document Validation Tests (Rule 4)

        [Fact]
        public async Task Handle_ShouldThrow_WhenRequiredDocumentMissing()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);            
            permitType.AddField("LotSize", FieldType.Number, false);

            permitType.AddDocumentRequirement("SitePlan", true, [".pdf"], 2048); // required document

            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Notes");
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);
            application.AddFieldValue("SitePlan", "site-plan.pdf", 1);
            application.AddFieldValue("LotSize", "5000", 2);
            // No document uploaded for "SitePlan"

            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("required documents", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SitePlan", exception.Message);
        }

        [Fact]
        public async Task Handle_ShouldPass_WhenRequiredDocumentsPresent()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);
            permitType.AddField("SitePlan", FieldType.FileUpload, true); // required document
            permitType.AddField("LotSize", FieldType.Number, false);

            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Notes");
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);
            application.AddFieldValue("SitePlan", "site-plan.pdf", 1);
            application.AddFieldValue("LotSize", "5000", 2);      
            // Upload matching document
            application.AddDocument(
                Guid.NewGuid(),"Building Permit", "SitePlan_v1.pdf", "application/pdf", 2048,
                "https://blob.url/siteplan.pdf", _testUserId);

            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act — should not throw
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
        }

        [Fact]
        public async Task Handle_ShouldPass_WhenOptionalDocumentsMissing()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);
            permitType.AddField("SitePlan", FieldType.FileUpload, true);  // required
            permitType.AddField("ExtraPhoto", FieldType.FileUpload, false); // optional — no upload needed

            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Notes");
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);
            application.AddFieldValue("SitePlan", "site-plan.pdf", 1);
            // Upload required document
            application.AddDocument(
                Guid.NewGuid(), "Building Permit", "SitePlan_final.pdf", "application/pdf", 2048,
                "https://blob.url/siteplan.pdf", _testUserId);
            // No upload for optional ExtraPhoto — should be ignored

            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act — should not throw
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
        }

        [Fact]
        public async Task Handle_ShouldReportAllMissingDocuments()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);
            permitType.AddField("ProofOfIdentity", FieldType.FileUpload, true);
            permitType.AddField("SitePlan", FieldType.FileUpload, true);
            permitType.AddField("EnvironmentalAssessment", FieldType.FileUpload, true);
            permitType.AddField("LotSize", FieldType.Number, false);

            permitType.AddDocumentRequirement("ProofOfIdentity", true, [".pdf"], 2048);
            permitType.AddDocumentRequirement("SitePlan", true, [".pdf"], 2048);            
            permitType.AddDocumentRequirement("EnvironmentalAssessment", true, [".pdf"], 2048);                                   

            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Notes");
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);
            application.AddFieldValue("ProofOfIdentity", "identity.pdf", 1);            
            application.AddFieldValue("SitePlan", "site-plan.pdf", 2);            
            application.AddFieldValue("EnvironmentalAssessment", "environment.pdf", 3);
            application.AddFieldValue("LotSize", "5000", 4);
            // No documents uploaded for any of the 3 required file fields

            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("ProofOfIdentity", exception.Message);
            Assert.Contains("SitePlan", exception.Message);
            Assert.Contains("EnvironmentalAssessment", exception.Message);
        }

        [Fact]
        public async Task Handle_ShouldPass_WhenDocumentTypeMatches_RegardlessOfFilename()
        {
            // Verify that the validation uses Document.DocumentType, not filename
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);
            permitType.AddDocumentRequirement("SitePlan", true, [".pdf"], 2048);
        
            var application = new ATLAS.Domain.Entities.Application(_testUserId, permitType.Id, "Notes");
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);
            // Upload with an arbitrary filename that doesn't hint at "SitePlan"
            application.AddDocument(
                Guid.NewGuid(),
                "SitePlan",                        // DocumentType matches the requirement
                "house-final-v7.pdf",              // arbitrary filename
                "application/pdf",
                2048,
                "https://blob.url/house-final-v7.pdf",
                _testUserId);
        
            _mockAppRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(permitType.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);
        
            var command = new SubmitDraftCommand { ApplicationId = Guid.NewGuid() };
        
            // Act — should not throw despite the arbitrary filename
            await _handler.Handle(command, CancellationToken.None);
        
            // Assert
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
        }

        #endregion
    }
}