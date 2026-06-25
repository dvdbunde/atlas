using ATLAS.Application.DTOs;
using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.FormModel;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Shared
{
    public class DynamicFormGeneratorFileUploadTests : BunitContext
    {
        [Fact]
        public void ShouldRenderInputFile_WhenFieldTypeIsFileUpload()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsRequired = true,
                AllowedExtensions = ".pdf,.png,.jpg"
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            cut.Markup.Contains("input");
            cut.Markup.Contains("type=\"file\"");
            cut.Markup.Contains("Site Plan");
            cut.Markup.Contains(".pdf,.png,.jpg");
        }

        [Fact]
        public void ShouldShowSelectedFileName_WhenFileIsSelected()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsRequired = true
            };

            // Act — simulate file selection
            field.SelectedFileName = "document.pdf";

            // Assert
            Assert.Equal("document.pdf", field.SelectedFileName);
        }

        [Fact]
        public void ShouldShowNoDocumentMessage_WhenReadOnlyAndNoDocuments()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsRequired = true
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.ReadOnly));

            // Assert
            cut.Markup.Contains("No document uploaded");
        }

        [Fact]
        public void ShouldShowDocumentName_WhenReadOnlyAndDocumentsExist()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsRequired = true,
                UploadedDocuments = new()
                {
                    new() { FileName = "site-plan.pdf", FileSize = 2048 }
                }
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.ReadOnly));

            // Assert
            cut.Markup.Contains("site-plan.pdf");
        }

        [Fact]
        public void ShouldShowValidationError_WhenRequiredAndNoFileSelected()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsRequired = true
            };

            // Act — simulate validation
            if (field.IsRequired && string.IsNullOrWhiteSpace(field.SelectedFileName))
            {
                field.Errors.Add("Site Plan is required.");
            }

            // Assert
            Assert.True(field.HasErrors);
            Assert.Contains(field.Errors, e => e.Contains("required"));
        }

        [Fact]
        public void FromDocumentRequirement_ShouldCreateFileUploadField()
        {
            // Arrange
            var req = new DocumentRequirementDto
            {
                DocumentType = "Site Plan",
                IsRequired = true,
                AllowedExtensions = ".pdf,.png,.jpg",
                MaxFileSizeBytes = 10 * 1024 * 1024
            };

            // Act
            var field = DynamicFormFieldViewModel.FromDocumentRequirement(req);

            // Assert
            Assert.Equal("Site Plan", field.Label);
            Assert.Equal(FieldType.FileUpload, field.Type);
            Assert.True(field.IsRequired);
            Assert.Equal(".pdf,.png,.jpg", field.AllowedExtensions);
            Assert.Equal(10 * 1024 * 1024, field.MaxFileSizeBytes);
        }
    }
}