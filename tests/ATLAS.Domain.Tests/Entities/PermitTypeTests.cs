using System;
using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Events;
using Xunit;
using ATLAS.Domain.Enums;

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

        [Fact]
        public void UpdateFee_WithValidFee_ShouldUpdateFeeAndRaiseEvent()
        {
            var permitType = new PermitType("Test Type", "Description", 100.00m);

            permitType.UpdateFee(250.50m);

            Assert.Equal(250.50m, permitType.Fee);
            var evt = Assert.Single(permitType.DomainEvents.OfType<PermitTypeFeeUpdatedEvent>());
            Assert.Equal(100.00m, evt.OldFee);
            Assert.Equal(250.50m, evt.NewFee);
        }

        [Fact]
        public void UpdateFee_WithNegativeFee_ShouldThrow()
        {
            var permitType = new PermitType("Test Type", "Description", 100.00m);

            Assert.Throws<ArgumentException>(() => permitType.UpdateFee(-1m));
        }

        #region UpdateGeneralInformation Tests

        [Fact]
        public void UpdateGeneralInformation_ShouldUpdateNameAndDescription()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.ClearDomainEvents();

            // Act
            permitType.UpdateGeneralInformation("Renovation Permit", "Updated description");

            // Assert
            Assert.Equal("Renovation Permit", permitType.Name);
            Assert.Equal("Updated description", permitType.Description);
            var evt = Assert.Single(permitType.DomainEvents);
            var updated = Assert.IsType<PermitTypeGeneralInformationUpdatedEvent>(evt);
            Assert.Equal(permitType.Id, updated.PermitTypeId);
            Assert.Equal("Renovation Permit", updated.Name);
            Assert.Equal("Updated description", updated.Description);
        }

        [Fact]
        public void UpdateGeneralInformation_ShouldThrow_WhenNameTooShort()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            Assert.Throws<ArgumentException>(() => permitType.UpdateGeneralInformation("AB", "Description"));
        }

        [Fact]
        public void UpdateGeneralInformation_ShouldThrow_WhenNameEmpty()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            Assert.Throws<ArgumentException>(() => permitType.UpdateGeneralInformation("   ", "Description"));
        }

        #endregion

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
            permitType.Activate(_adminId);

            // Assert
            Assert.True(permitType.IsActive);
        }

        [Fact]
        public void Activate_ShouldNotChange_WhenAlreadyActive()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            // Act
            permitType.Activate(_adminId);

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

        #region Field Editing Tests

        [Fact]
        public void AddField_ShouldAssignContiguousOrder()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            permitType.AddField("FieldA", FieldType.Text, true);
            permitType.AddField("FieldB", FieldType.Text, false);

            Assert.Equal(1, permitType.Fields[0].Order);
            Assert.Equal(2, permitType.Fields[1].Order);
        }

        [Fact]
        public void UpdateField_ShouldModifyFieldAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("FieldA", FieldType.Text, true);
            var fieldId = permitType.Fields[0].Id;
            permitType.ClearDomainEvents();

            permitType.UpdateField(fieldId, "Renamed", FieldType.Number, false);

            var field = permitType.Fields.Single();
            Assert.Equal("Renamed", field.Name);
            Assert.Equal(FieldType.Number, field.Type);
            Assert.False(field.IsRequired);
            var evt = Assert.Single(permitType.DomainEvents);
            var updated = Assert.IsType<PermitTypeFieldUpdatedEvent>(evt);
            Assert.Equal(fieldId, updated.FieldId);
            Assert.Equal("Renamed", updated.FieldName);
        }

        [Fact]
        public void UpdateField_ShouldThrow_WhenDuplicateName()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("FieldA", FieldType.Text, true);
            permitType.AddField("FieldB", FieldType.Text, false);
            var bId = permitType.Fields[1].Id;

            var exception = Assert.Throws<DomainException>(() =>
                permitType.UpdateField(bId, "FieldA", FieldType.Text, false));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void RemoveField_ShouldRemoveAndRenumberAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("FieldA", FieldType.Text, true);
            permitType.AddField("FieldB", FieldType.Text, false);
            permitType.AddField("FieldC", FieldType.Text, false);
            var bId = permitType.Fields[1].Id;
            permitType.ClearDomainEvents();

            permitType.RemoveField(bId);

            Assert.Equal(2, permitType.Fields.Count);
            Assert.DoesNotContain(permitType.Fields, f => f.Id == bId);
            Assert.Equal(1, permitType.Fields[0].Order);
            Assert.Equal(2, permitType.Fields[1].Order);
            var evt = Assert.Single(permitType.DomainEvents);
            var removed = Assert.IsType<PermitTypeFieldRemovedEvent>(evt);
            Assert.Equal(bId, removed.FieldId);
        }

        [Fact]
        public void RemoveField_ShouldThrow_WhenNotFound()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            Assert.Throws<DomainException>(() => permitType.RemoveField(Guid.NewGuid()));
        }

        [Fact]
        public void MoveField_ShouldReorderAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("FieldA", FieldType.Text, true);
            permitType.AddField("FieldB", FieldType.Text, false);
            permitType.AddField("FieldC", FieldType.Text, false);
            var aId = permitType.Fields[0].Id;
            permitType.ClearDomainEvents();

            permitType.MoveField(aId, 3);

            Assert.Equal("FieldB", permitType.Fields[0].Name);
            Assert.Equal("FieldC", permitType.Fields[1].Name);
            Assert.Equal("FieldA", permitType.Fields[2].Name);
            Assert.Equal(3, permitType.Fields[2].Order);
            var evt = Assert.Single(permitType.DomainEvents);
            Assert.IsType<PermitTypeFieldsReorderedEvent>(evt);
        }

        [Fact]
        public void MoveField_ShouldThrow_WhenOrderOutOfRange()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddField("FieldA", FieldType.Text, true);
            var aId = permitType.Fields[0].Id;

            Assert.Throws<DomainException>(() => permitType.MoveField(aId, 5));
        }

        #endregion

        #region Document Requirement Editing Tests

        [Fact]
        public void AddDocumentRequirement_ShouldAssignContiguousOrder()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);

            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            permitType.AddDocumentRequirement("Photo", false, new[] { ".png" }, 2000);

            Assert.Equal(1, permitType.DocumentRequirements[0].Order);
            Assert.Equal(2, permitType.DocumentRequirements[1].Order);
        }

        [Fact]
        public void UpdateDocumentRequirement_ShouldModifyAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            var reqId = permitType.DocumentRequirements[0].Id;
            permitType.ClearDomainEvents();

            permitType.UpdateDocumentRequirement(reqId, false, new[] { ".pdf", ".jpg" }, 5000);

            var req = permitType.DocumentRequirements.Single();
            Assert.False(req.IsRequired);
            Assert.Equal(5000, req.MaxFileSizeBytes);
            var evt = Assert.Single(permitType.DomainEvents);
            var updated = Assert.IsType<PermitTypeDocumentRequirementUpdatedEvent>(evt);
            Assert.Equal(reqId, updated.DocumentRequirementId);
        }

        [Fact]
        public void RemoveDocumentRequirement_ShouldRemoveAndRenumberAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            permitType.AddDocumentRequirement("Photo", false, new[] { ".png" }, 2000);
            var id = permitType.DocumentRequirements[0].Id;
            permitType.ClearDomainEvents();

            permitType.RemoveDocumentRequirement(id);

            Assert.Single(permitType.DocumentRequirements);
            Assert.DoesNotContain(permitType.DocumentRequirements, d => d.Id == id);
            Assert.Equal(1, permitType.DocumentRequirements[0].Order);
            var evt = Assert.Single(permitType.DomainEvents);
            Assert.IsType<PermitTypeDocumentRequirementRemovedEvent>(evt);
        }

        [Fact]
        public void MoveDocumentRequirement_ShouldReorderAndRaiseEvent()
        {
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            permitType.AddDocumentRequirement("Photo", false, new[] { ".png" }, 2000);
            var photoId = permitType.DocumentRequirements[1].Id;
            permitType.ClearDomainEvents();

            permitType.MoveDocumentRequirement(photoId, 1);

            Assert.Equal("Photo", permitType.DocumentRequirements[0].DocumentType);
            Assert.Equal(1, permitType.DocumentRequirements[0].Order);
            var evt = Assert.Single(permitType.DomainEvents);
            Assert.IsType<PermitTypeDocumentRequirementsReorderedEvent>(evt);
        }

        #endregion
    }
}
