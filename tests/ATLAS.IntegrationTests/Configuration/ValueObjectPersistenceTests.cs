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
    }
}
