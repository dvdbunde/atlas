using System;
using ATLAS.Domain.ValueObjects;
using Xunit;

namespace ATLAS.Domain.Tests.ValueObjects
{
    public class DocumentRequirementTests
    {
        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var req = new DocumentRequirement(
                "BuildingPlan", 
                true, 
                new string[] { ".pdf", ".dwg" }, 
                10L * 1024 * 1024);

            // Assert
            Assert.Equal("BuildingPlan", req.DocumentType);
            Assert.True(req.IsRequired);
            Assert.Equal(2, req.AllowedExtensions.Length);
            Assert.Equal(".pdf", req.AllowedExtensions[0]);
            Assert.Equal(10L * 1024 * 1024, req.MaxFileSizeBytes);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenDocumentTypeIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new DocumentRequirement("", true, new string[] { ".pdf" }, 1024));
            Assert.Contains("Document type cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenAllowedExtensionsIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new DocumentRequirement("Doc", true, new string[0], 1024));
            Assert.Contains("Allowed extensions must be provided", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenMaxFileSizeIsZero()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new DocumentRequirement("Doc", true, new string[] { ".pdf" }, 0));
            Assert.Contains("Max file size must be positive", exception.Message);
        }

        [Fact]
        public void Equals_ShouldReturnTrue_WhenSameValues()
        {
            // Arrange
            var req1 = new DocumentRequirement("Plan", true, new string[] { ".pdf" }, 1024);
            var req2 = new DocumentRequirement("Plan", true, new string[] { ".pdf" }, 1024);

            // Act & Assert
            Assert.True(req1.Equals(req2));
        }
    }
}
