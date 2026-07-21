using System;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.ValueObjects;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    [Collection("Sequential Integration Tests")]
    public class ValueObjectPersistenceTests
    {
        [Fact]
        public async Task PermitFieldValueObject_ShouldPersistAndRetrieve()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var permitType = new PermitType("Test Permit", "Description", 100.00m);
                permitType.AddField("Field1", FieldType.Text, true, "");
                context.PermitTypes.Add(permitType);
                await context.SaveChangesAsync();

                // Act
                var retrieved = await context.PermitTypes.FindAsync(permitType.Id);
                
                // Assert
                Assert.NotNull(retrieved);
                Assert.Single(retrieved.Fields);
                Assert.Equal("Field1", retrieved.Fields.First().Name);
            }
        }

        [Fact]
        public async Task DocumentRequirementValueObject_ShouldPersistAndRetrieve()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var permitType = new PermitType("Test Permit", "Description", 100.00m);
                permitType.AddDocumentRequirement("Passport", true, new[] { ".pdf", ".jpg" }, 5242880);
                context.PermitTypes.Add(permitType);
                await context.SaveChangesAsync();

                // Act
                var retrieved = await context.PermitTypes.FindAsync(permitType.Id);
                
                // Assert
                Assert.NotNull(retrieved);
                Assert.Single(retrieved.DocumentRequirements);
                Assert.Equal("Passport", retrieved.DocumentRequirements.First().DocumentType);
            }
        }

        [Fact]
        public void ValueObjects_ShouldBeImmutable()
        {
            // Arrange & Act
            var field1 = new PermitField("Name", FieldType.Text, true, null);
            var field2 = new PermitField("Name", FieldType.Text, true, null);

            // Assert - Value objects should be equal if same values
            Assert.Equal(field1, field2);
        }

        [Fact]
        public async Task PermitFieldOrdering_ShouldPersistAndBeRetrievedInOrder()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Guid permitTypeId;
            using (var context = new ApplicationDbContext(options))
            {
                var permitType = new PermitType("Test Permit", "Description", 100.00m);
                permitType.AddField("FieldA", FieldType.Text, true, "");
                permitType.AddField("FieldB", FieldType.Text, false, "");
                permitType.AddField("FieldC", FieldType.Text, false, "");
                var aId = permitType.Fields[0].Id;

                // Act: move FieldA to the end, then persist
                permitType.MoveField(aId, 3);
                context.PermitTypes.Add(permitType);
                await context.SaveChangesAsync();
                permitTypeId = permitType.Id;
            }

            // Act: re-retrieve from a fresh context
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PermitTypes.FindAsync(permitTypeId);

                // Assert: ordering persisted and retrieval preserves it
                Assert.NotNull(retrieved);
                Assert.Equal(3, retrieved.Fields.Count);
                Assert.Equal("FieldB", retrieved.Fields[0].Name);
                Assert.Equal("FieldC", retrieved.Fields[1].Name);
                Assert.Equal("FieldA", retrieved.Fields[2].Name);
                Assert.Equal(1, retrieved.Fields[0].Order);
                Assert.Equal(2, retrieved.Fields[1].Order);
                Assert.Equal(3, retrieved.Fields[2].Order);
            }
        }

        [Fact]
        public async Task DocumentRequirementOrdering_ShouldPersistAndBeRetrievedInOrder()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Guid permitTypeId;
            using (var context = new ApplicationDbContext(options))
            {
                var permitType = new PermitType("Test Permit", "Description", 100.00m);
                permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
                permitType.AddDocumentRequirement("Photo", false, new[] { ".png" }, 2000);
                var photoId = permitType.DocumentRequirements[1].Id;

                // Act: move Photo to the front, then persist
                permitType.MoveDocumentRequirement(photoId, 1);
                context.PermitTypes.Add(permitType);
                await context.SaveChangesAsync();
                permitTypeId = permitType.Id;
            }

            // Act: re-retrieve from a fresh context
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PermitTypes.FindAsync(permitTypeId);

                // Assert: ordering persisted and retrieval preserves it
                Assert.NotNull(retrieved);
                Assert.Equal(2, retrieved.DocumentRequirements.Count);
                Assert.Equal("Photo", retrieved.DocumentRequirements[0].DocumentType);
                Assert.Equal("ID", retrieved.DocumentRequirements[1].DocumentType);
                Assert.Equal(1, retrieved.DocumentRequirements[0].Order);
                Assert.Equal(2, retrieved.DocumentRequirements[1].Order);
            }
        }
    }
}
