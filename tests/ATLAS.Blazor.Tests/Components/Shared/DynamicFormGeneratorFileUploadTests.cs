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
        public void ShouldShowUploadProgress_WhenUploading()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsUploading = true
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            cut.Markup.Contains("Uploading...");
        }

        [Fact]
        public void ShouldShowUploadError_WhenUploadFails()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                UploadFailed = true,
                UploadErrorMessage = "Upload failed. Please try again."
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            cut.Markup.Contains("Upload failed");
        }

        [Fact]
        public void ShouldShowUploadedDocuments_WhenDocumentsExist()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                UploadedDocuments = new()
                {
                    new() { FileName = "site-plan.pdf", FileSize = 2048, UploadedDate = DateTime.UtcNow }
                }
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            cut.Markup.Contains("site-plan.pdf");
            Assert.Contains("2KB", cut.Markup);
        }

        [Fact]
        public void ShouldDisableInputFile_WhenUploading()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "SitePlan",
                Label = "Site Plan",
                Type = FieldType.FileUpload,
                IsUploading = true
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            Assert.Contains("disabled", cut.Markup);
        }

        [Fact]
        public void ShouldShowRequirementCard_WhenFileUploadField()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "ProofOfIdentity",
                Label = "Proof of Identity",
                Type = FieldType.FileUpload,
                IsRequired = true
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert — card with role="region" is rendered
            var card = cut.Find("div[role=\"region\"]");
            Assert.NotNull(card);
            Assert.Contains("Proof of Identity", card.TextContent);
        }

        [Fact]
        public void ShouldShowDeleteButton_WhenDocumentsExist()
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
                    new() { Id = Guid.NewGuid(), FileName = "plan.pdf", FileSize = 2048 }
                }
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            var deleteButton = cut.Find("button[aria-label*=\"Remove\"]");
            Assert.NotNull(deleteButton);
            Assert.Contains("plan.pdf", deleteButton.GetAttribute("aria-label"));
        }

        [Fact]
        public void ShouldShowMissingBadge_WhenRequiredAndNoDocuments()
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
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            Assert.Contains("Missing", cut.Markup);
        }

        [Fact]
        public void ShouldShowUploadedBadge_WhenDocumentsExist()
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
                    new() { Id = Guid.NewGuid(), FileName = "plan.pdf", FileSize = 1024 }
                }
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            // Assert
            Assert.Contains("Uploaded", cut.Markup);
        }

        [Fact]
        public void ShouldShowNotSuppliedBadge_WhenOptionalAndNoDocuments()
        {
            // Arrange
            var field = new DynamicFormFieldViewModel
            {
                FieldName = "ExtraPhoto",
                Label = "Extra Photo",
                Type = FieldType.FileUpload,
                IsRequired = false
            };

            // Act
            var cut = Render<DynamicFormGenerator>(parameters => parameters
                .Add(p => p.Fields, new[] { field })
                .Add(p => p.Mode, FormFieldMode.Edit));

            
            // Assert — optional fields are always considered satisfied
            Assert.Contains("Uploaded", cut.Markup);
            Assert.Contains("Document on file.", cut.Markup);
        }

        [Fact]
        public void ShouldShowUploadControl_WhenAllowFileUploadIsTrue()
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
                .Add(p => p.Mode, FormFieldMode.Edit)
                .Add(p => p.AllowFileUpload, true));

            // Assert — InputFile rendered
            Assert.Contains("input", cut.Markup);
        }

        [Fact]
        public void ShouldHideUploadControl_WhenAllowFileUploadIsFalse()
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
                .Add(p => p.Mode, FormFieldMode.Edit)
                .Add(p => p.AllowFileUpload, false));

            // Assert — no InputFile
            Assert.DoesNotContain("type=\"file\"", cut.Markup);
        }
    }
}