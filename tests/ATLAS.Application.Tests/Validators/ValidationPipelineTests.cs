using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Commands.Documents;
using ATLAS.Application.Commands.Validators;
using FluentValidation;
using Xunit;

namespace ATLAS.Application.Tests.Validators
{
    public class ValidationPipelineTests
    {        
        #region ApproveApplicationCommandValidator Tests

        [Fact]
        public void ApproveApplicationCommand_ShouldPass_WhenValid()
        {
            // Arrange
            var validator = new ApproveApplicationCommandValidator();
            var command = new ApproveApplicationCommand
            {
                ApplicationId = Guid.NewGuid(),
                Comments = "Approved"
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ApproveApplicationCommand_ShouldFail_WhenApplicationIdEmpty()
        {
            // Arrange
            var validator = new ApproveApplicationCommandValidator();
            var command = new ApproveApplicationCommand
            {
                ApplicationId = Guid.Empty
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ApplicationId" && e.ErrorMessage == "ApplicationId is required");
        }

        #endregion

        #region RejectApplicationCommandValidator Tests (Security - PRD)

        [Fact]
        public void RejectApplicationCommand_ShouldFail_WhenReasonCodeEmpty()
        {
            // Arrange
            var validator = new RejectApplicationCommandValidator();
            var command = new RejectApplicationCommand
            {
                ApplicationId = Guid.NewGuid(),
                ReasonCode = ""
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ReasonCode" && e.ErrorMessage == "Rejection reason code is required");
        }

        [Fact]
        public void RejectApplicationCommand_ShouldFail_WhenReasonCodeTooLong()
        {
            // Arrange
            var validator = new RejectApplicationCommandValidator();
            var command = new RejectApplicationCommand
            {
                ApplicationId = Guid.NewGuid(),
                ReasonCode = new string('R', 1001) // Exceeds 1000 chars
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ReasonCode" && e.ErrorMessage == "Rejection reason code cannot exceed 1000 characters");
        }

        #endregion

        #region UploadDocumentCommandValidator Tests (Security - PRD)

        [Fact]
        public void UploadDocumentCommand_ShouldFail_WhenFileSizeExceeds10MB()
        {
            // Arrange
            var validator = new UploadDocumentCommandValidator();
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 35 * 1024 * 1024,
                FileContent = new MemoryStream()
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "FileSize" && e.ErrorMessage.Contains("File size cannot exceed"));
        }

        [Fact]
        public void UploadDocumentCommand_ShouldFail_WhenInvalidContentType()
        {
            // Arrange
            var validator = new UploadDocumentCommandValidator();
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileName = "test.exe",
                ContentType = "application/exe", // Invalid type
                FileSize = 1024,
                FileContent = new MemoryStream(),                
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ContentType" && e.ErrorMessage.Contains("Content type must be one of"));
        }

        [Fact]
        public void UploadDocumentCommand_ShouldPass_WhenValidPdf()
        {
            // Arrange
            var validator = new UploadDocumentCommandValidator();
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                DocumentType = "Building Permit",
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                FileContent = new MemoryStream()                
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
        }

        #endregion
    }
}
