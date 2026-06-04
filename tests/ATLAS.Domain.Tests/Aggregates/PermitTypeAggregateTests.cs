using System;
using System.Collections.Generic;
using ATLAS.Domain.Aggregates;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using Xunit;

namespace ATLAS.Domain.Tests.Aggregates
{
    public class PermitTypeAggregateTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithPermitType()
        {
            // Arrange & Act
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);

            // Assert
            Assert.Equal(permitType, aggregate.PermitType);
            Assert.Empty(aggregate.Fields);
            Assert.Empty(aggregate.DocumentRequirements);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenPermitTypeIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new PermitTypeAggregate(null!));
            Assert.Contains("permitType", exception.Message);
        }

        [Fact]
        public void AddField_ShouldAddField_WhenValid()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            var field = new PermitField("PropertyAddress", FieldType.Text, true, null);

            // Act
            aggregate.AddField(field);

            // Assert
            Assert.Single(aggregate.Fields);
            Assert.Equal(field.Name, aggregate.Fields[0].Name);
        }

        [Fact]
        public void AddField_ShouldThrowException_WhenDuplicateFieldName()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            var field1 = new PermitField("PropertyAddress", FieldType.Text, true, null);
            var field2 = new PermitField("PropertyAddress", FieldType.Text, true, null);
            
            aggregate.AddField(field1);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.AddField(field2));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void AddDocumentRequirement_ShouldAddRequirement_WhenValid()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            var requirement = new DocumentRequirement("PDF", true, new string[] { ".pdf" }, 10 * 1024 * 1024);

            // Act
            aggregate.AddDocumentRequirement(requirement);

            // Assert
            Assert.Single(aggregate.DocumentRequirements);
            Assert.Equal(requirement.DocumentType, aggregate.DocumentRequirements[0].DocumentType);
        }

        [Fact]
        public void AddDocumentRequirement_ShouldThrowException_WhenDuplicateDocumentType()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            var req1 = new DocumentRequirement("PDF", true, new string[] { ".pdf" }, 10 * 1024 * 1024);
            var req2 = new DocumentRequirement("PDF", true, new string[] { ".pdf" }, 10 * 1024 * 1024);
            
            aggregate.AddDocumentRequirement(req1);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.AddDocumentRequirement(req2));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public void ValidateInvariants_ShouldPass_WhenValidState()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            aggregate.AddField(new PermitField("Address", FieldType.Text, true, null));
            aggregate.AddDocumentRequirement(new DocumentRequirement("PDF", true, new string[] { ".pdf" }, 10 * 1024 * 1024));

            // Act & Assert (should not throw)
            aggregate.ValidateInvariants();
        }

        [Fact]
        public void ValidateInvariants_ShouldThrowException_WhenDuplicateFieldNames()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            var aggregate = new PermitTypeAggregate(permitType);
            aggregate.AddField(new PermitField("Address", FieldType.Text, true, null));
            aggregate.AddField(new PermitField("Address", FieldType.Text, true, null));

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.ValidateInvariants());
            Assert.Contains("Duplicate field names", exception.Message);
        }
    }
}
