using System;
using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class ApplicationFieldValueTests
    {
        private readonly Guid _applicationId = Guid.NewGuid();

        [Fact]
        public void Constructor_ShouldInitializeWithValidValues()
        {
            //Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");

            // Act
            var added = application.AddFieldValue("PropertyAddress", "123 Main St", 0);

            // Assert
            // Access via Application since constructor is internal                       
            Assert.Equal("PropertyAddress", added.FieldName);
            Assert.Equal("123 Main St", added.Value);
            Assert.Equal(0, added.SortOrder);
        }

        [Fact]
        public void Constructor_ShouldConvertNullValueToEmptyString()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");

            // Act
            var fieldValue = application.AddFieldValue("FieldName", null, 0);

            // Assert
            Assert.Equal(string.Empty, fieldValue.Value);
        }

        [Fact]
        public void UpdateValue_ShouldChangeValue()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");
            application.AddFieldValue("FieldName", "initial", 0);

            // Act
            application.UpdateFieldValue("FieldName", "updated");

            // Assert
            Assert.Equal("updated", application.FieldValues[0].Value);
        }

        [Fact]
        public void UpdateValue_ShouldConvertNullToEmptyString()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");
            application.AddFieldValue("FieldName", "initial", 0);

            // Act
            application.UpdateFieldValue("FieldName", null);

            // Assert
            Assert.Equal(string.Empty, application.FieldValues[0].Value);
        }

        [Fact]
        public void AddFieldValue_ShouldThrowException_WhenFieldNameAlreadyExists()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");
            application.AddFieldValue("FieldName", "value1", 0);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AddFieldValue("FieldName", "value2", 1));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void UpdateFieldValue_ShouldThrowException_WhenFieldNotFound()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.UpdateFieldValue("NonExistent", "value"));
            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public void RemoveFieldValue_ShouldRemoveField()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");
            application.AddFieldValue("FieldName", "value", 0);

            // Act
            application.RemoveFieldValue("FieldName");

            // Assert
            Assert.Empty(application.FieldValues);
        }

        [Fact]
        public void RemoveFieldValue_ShouldThrowException_WhenFieldNotFound()
        {
            // Arrange
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "test");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.RemoveFieldValue("NonExistent"));
            Assert.Contains("not found", exception.Message);
        }
    }
}