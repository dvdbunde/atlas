using System;
using ATLAS.Domain.Enums;
using ATLAS.Domain.ValueObjects;
using Xunit;

namespace ATLAS.Domain.Tests.ValueObjects
{
    public class PermitFieldTests
    {
        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var field = new PermitField("PropertyAddress", FieldType.Text, true, "Default");

            // Assert
            Assert.Equal("PropertyAddress", field.Name);
            Assert.Equal(FieldType.Text, field.Type);
            Assert.True(field.IsRequired);
            Assert.Equal("Default", field.DefaultValue);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PermitField("", FieldType.Text, true));
            Assert.Contains("Field name cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenNameTooShort()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PermitField("A", FieldType.Text, true));
            Assert.Contains("between 2 and 100 characters", exception.Message);
        }

        [Fact]
        public void Equals_ShouldReturnTrue_WhenSameValues()
        {
            // Arrange
            var field1 = new PermitField("Address", FieldType.Text, true, null);
            var field2 = new PermitField("Address", FieldType.Text, true, null);

            // Act & Assert
            Assert.True(field1.Equals(field2));
            Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
        }

        [Fact]
        public void Equals_ShouldReturnFalse_WhenDifferentValues()
        {
            // Arrange
            var field1 = new PermitField("Address", FieldType.Text, true, null);
            var field2 = new PermitField("Address", FieldType.Number, true, null);

            // Act & Assert
            Assert.False(field1.Equals(field2));
        }
    }
}
