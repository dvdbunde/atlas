using System;
using System.IO;
using System.Text;
using ATLAS.Application.Commands.Documents;
using ATLAS.Application.Commands.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace ATLAS.Application.Tests.Validators
{
    public class UploadDocumentCommandValidatorTests
    {
        private readonly UploadDocumentCommandValidator _validator;

        public UploadDocumentCommandValidatorTests()
        {
            _validator = new UploadDocumentCommandValidator();
        }

        [Fact]
        public void ShouldHaveError_WhenApplicationIdIsEmpty()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.Empty,
                FileContent = new MemoryStream(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
        }

        [Fact]
        public void ShouldHaveError_WhenFileNameIsEmpty()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(),
                FileName = "",
                ContentType = "application/pdf",
                FileSize = 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }

        [Fact]
        public void ShouldHaveError_WhenContentTypeIsNotAllowed()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(),
                FileName = "test.exe",
                ContentType = "application/x-msdownload",
                FileSize = 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ContentType);
        }

        [Fact]
        public void ShouldHaveError_WhenFileSizeExceeds25MB()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 26 * 1024 * 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FileSize);
        }

        [Fact]
        public void ShouldNotHaveError_WhenCommandIsValid()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(Encoding.UTF8.GetBytes("test")),
                FileName = "document.pdf",
                ContentType = "application/pdf",
                FileSize = 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ShouldHaveError_WhenFileSizeIsZero()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(),
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSize = 0
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FileSize);
        }

        [Fact]
        public void ShouldHaveError_WhenFileNameHasNoExtension()
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = Guid.NewGuid(),
                FileContent = new MemoryStream(),
                FileName = "noextension",
                ContentType = "application/pdf",
                FileSize = 1024
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.FileName);
        }
    }
}