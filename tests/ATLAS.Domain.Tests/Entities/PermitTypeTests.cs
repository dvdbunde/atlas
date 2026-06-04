using System;
using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Events;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class PermitTypeTests
    {
        private readonly Guid _adminId = Guid.NewGuid();

        [Fact]
        public void Create_ShouldInitializeWithActiveStatus()
        {
            // Act
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Assert
            Assert.Equal("Building Permit", permitType.Name);
            Assert.Equal("Description", permitType.Description);
            Assert.Equal(100.00m, permitType.Fee);
            Assert.True(permitType.IsActive);
        }

        #region AddField Tests

        [Fact]
        public void AddField_ShouldAddFieldSuccessfully()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Act
            permitType.AddField("PropertyAddress", FieldType.Text, true, null);

            // Assert
            var field = Assert.Single(permitType.Fields);
            Assert.Equal("PropertyAddress", field.Name);
            Assert.Equal(FieldType.Text, field.Type);
            Assert.True(field.IsRequired);
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
        public void AddField_ShouldRaisePermitTypeFieldAddedEvent()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.ClearDomainEvents();

            // Act
            permitType.AddField("PropertyAddress", FieldType.Text, true, null);

            // Assert
            var domainEvent = Assert.Single(permitType.DomainEvents);
            var fieldAddedEvent = Assert.IsType<PermitTypeFieldAddedEvent>(domainEvent);
            Assert.Equal(permitType.Id, fieldAddedEvent.PermitTypeId);
            Assert.Equal("PropertyAddress", fieldAddedEvent.FieldName);
            Assert.Equal(FieldType.Text, fieldAddedEvent.FieldType);
        }

        #endregion

        #region AddDocumentRequirement Tests

        [Fact]
        public void AddDocumentRequirement_ShouldAddSuccessfully()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Act
            permitType.AddDocumentRequirement("PDF", true, new[] { ".pdf" }, 5 * 1024 * 1024);

            // Assert
            var req = Assert.Single(permitType.DocumentRequirements);
            Assert.Equal("PDF", req.DocumentType);
            Assert.True(req.IsRequired);
            Assert.Contains(".pdf", req.AllowedExtensions);
        }

        [Fact]
        public void AddDocumentRequirement_ShouldThrowException_WhenDocumentTypeAlreadyExists()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddDocumentRequirement("PDF", true, new[] { ".pdf" }, 5 * 1024 * 1024);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                permitType.AddDocumentRequirement("PDF", false, new[] { ".pdf" }, 10 * 1024 * 1024));
            Assert.Contains("already exists", exception.Message);
        }

        #endregion

        #region Activate/Deactivate Tests

        [Fact]
        public void Activate_ShouldSetIsActiveToTrue()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.Deactivate(_adminId);

            // Act
            permitType.Activate();

            // Assert
            Assert.True(permitType.IsActive);
        }

        [Fact]
        public void Activate_ShouldNotChange_WhenAlreadyActive()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Act
            permitType.Activate();

            // Assert
            Assert.True(permitType.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Act
            permitType.Deactivate(_adminId);

            // Assert
            Assert.False(permitType.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldNotChange_WhenAlreadyInactive()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.Deactivate(_adminId);

            // Act
            permitType.Deactivate(_adminId);

            // Assert
            Assert.False(permitType.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldRaisePermitTypeDeactivatedEvent()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.ClearDomainEvents();

            // Act
            permitType.Deactivate(_adminId);

            // Assert
            var domainEvent = Assert.Single(permitType.DomainEvents);
            var deactivatedEvent = Assert.IsType<PermitTypeDeactivatedEvent>(domainEvent);
            Assert.Equal(permitType.Id, deactivatedEvent.PermitTypeId);
            Assert.Equal(_adminId, deactivatedEvent.DeactivatedByAdminId);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void Create_ShouldThrowException_WhenNameTooShort()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PermitType("AB", "Description", 100.00m));
            Assert.Contains("between 3 and 100 characters", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFeeNegative()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PermitType("Building Permit", "Description", -1.00m));
            Assert.Contains("cannot be negative", exception.Message);
        }

        #endregion
    }
}
