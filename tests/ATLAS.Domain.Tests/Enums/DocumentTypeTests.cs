using System;
using ATLAS.Domain.Enums;
using Xunit;

namespace ATLAS.Domain.Tests.Enums
{
    public class DocumentTypeTests
    {
        [Fact]
        public void DocumentType_ShouldBeMarkedObsolete()
        {
            // Assert
            var attribute = Attribute.GetCustomAttribute(typeof(DocumentType), typeof(ObsoleteAttribute));
            Assert.NotNull(attribute);
            Assert.IsType<ObsoleteAttribute>(attribute);
        }

        [Fact]
        public void DocumentType_ShouldPreserveAllExistingValues()
        {
            Assert.Equal(1, (int)DocumentType.PDF);
            Assert.Equal(2, (int)DocumentType.JPG);
            Assert.Equal(3, (int)DocumentType.PNG);
        }

        [Fact]
        public void DocumentType_ObsoleteMessage_ShouldReferenceMimeTypeAndDocumentRequirement()
        {
            // Assert
            var attribute = (ObsoleteAttribute?)Attribute.GetCustomAttribute(typeof(DocumentType), typeof(ObsoleteAttribute));
            Assert.NotNull(attribute);
            Assert.Contains("MIME", attribute!.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DocumentRequirement", attribute.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}