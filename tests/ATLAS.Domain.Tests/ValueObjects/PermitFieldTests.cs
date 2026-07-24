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
            var field = new PermitField("PropertyAddress", FieldType.Text, true, "Default", new List<string>());

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
                new PermitField("", FieldType.Text, true, string.Empty, new List<string>()));
            Assert.Contains("Field name cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenNameTooShort()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PermitField("A", FieldType.Text, true, string.Empty, new List<string>()));
            Assert.Contains("between 2 and 100 characters", exception.Message);
        }

        [Fact]
        public void Equals_ShouldReturnTrue_WhenSameValues()
        {
            // Arrange
            var field1 = new PermitField("Address", FieldType.Text, true, string.Empty, new List<string>());
            var field2 = new PermitField("Address", FieldType.Text, true, string.Empty, new List<string>());

            // Act & Assert
            Assert.True(field1.Equals(field2));
            Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
        }

        [Fact]
        public void Equals_ShouldReturnFalse_WhenDifferentValues()
        {
            // Arrange
            var field1 = new PermitField("Address", FieldType.Text, true, string.Empty, new List<string>());
            var field2 = new PermitField("Address", FieldType.Number, true, string.Empty, new List<string>());

            // Act & Assert
            Assert.False(field1.Equals(field2));
        }

        [Fact]
        public void Create_Dropdown_ShouldThrowException_WhenNoOptions()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new PermitField("BuildingType", FieldType.Dropdown, true, null, null));
            Assert.Contains("at least one option", exception.Message);
        }
        
        [Fact]
        public void Create_Dropdown_ShouldThrowException_WhenEmptyOptions()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new PermitField("BuildingType", FieldType.Dropdown, true, null, Array.Empty<string>()));
            Assert.Contains("at least one option", exception.Message);
        }
        
        [Fact]
        public void Create_Dropdown_ShouldThrowException_WhenDefaultValueNotInOptions()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new PermitField("BuildingType", FieldType.Dropdown, true, "Invalid",
                    new[] { "Residential", "Commercial" }));
            Assert.Contains("DefaultValue", exception.Message);
        }
        
        [Fact]
        public void Create_Dropdown_ShouldSucceed_WhenDefaultValueInOptions()
        {
            var field = new PermitField("BuildingType", FieldType.Dropdown, true, "Residential",
                new[] { "Residential", "Commercial" });
        
            Assert.Equal("Residential", field.DefaultValue);
            Assert.Equal(2, field.Options.Count);
        }
        
        [Fact]
        public void Equals_ShouldIncludeOptions()
        {
            var options = new[] { "A", "B" };
            var field1 = new PermitField("Type", FieldType.Dropdown, true, null, options);
            var field2 = new PermitField("Type", FieldType.Dropdown, true, null, options);
        
            Assert.True(field1.Equals(field2));
            Assert.Equal(field1.GetHashCode(), field2.GetHashCode());
        }
        
        [Fact]
        public void Equals_ShouldReturnFalse_WhenOptionsDiffer()
        {
            var field1 = new PermitField("Type", FieldType.Dropdown, true, null, new[] { "A", "B" });
            var field2 = new PermitField("Type", FieldType.Dropdown, true, null, new[] { "A", "C" });
        
            Assert.False(field1.Equals(field2));
        }
    }
}
