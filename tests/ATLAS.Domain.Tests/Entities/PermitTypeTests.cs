using System;
using System.Collections.Generic;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class PermitTypeTests
    {
        [Fact]
        public void Create_ShouldInitializeWithActiveStatus()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            Assert.Equal("Building Permit", permitType.Name);
            Assert.Equal("Description", permitType.Description);
            Assert.Equal(100.00m, permitType.Fee);
            Assert.True(permitType.IsActive);
        }

        [Fact]
        public void AddField_ShouldAddFieldSuccessfully()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var field = new PermitField("PropertyAddress", FieldType.Text, true, null);

            // Act - Call with individual parameters, not PermitField object
            permitType.AddField(field.Name, field.Type, field.IsRequired, field.DefaultValue);

            // Assert
            Assert.Single(permitType.Fields);
        }

        [Fact]
        public void AddField_ShouldThrowException_WhenFieldNameAlreadyExists()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("PropertyAddress", FieldType.Text, true);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                permitType.AddField("PropertyAddress", FieldType.Text, false));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.Deactivate(Guid.NewGuid());

            permitType.Activate();

            Assert.True(permitType.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var adminId = Guid.NewGuid();

            permitType.Deactivate(adminId);

            Assert.False(permitType.IsActive);
        }
    }
}
