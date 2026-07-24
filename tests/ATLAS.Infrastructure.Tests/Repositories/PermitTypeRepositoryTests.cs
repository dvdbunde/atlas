using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class PermitTypeRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingPermitType_ShouldReturnPermitType()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var permitType = new PermitType("Building Permit", "Description", 100.00m);
            context.PermitTypes.Add(permitType);
            await context.SaveChangesAsync();

            var repository = new PermitTypeRepository(context);

            // Act
            var result = await repository.GetByIdAsync(permitType.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(permitType.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repository = new PermitTypeRepository(context);

            // Act
            var result = await repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllPermitTypes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var pt1 = new PermitType("Type1", "Desc1", 50.00m);
            var pt2 = new PermitType("Type2", "Desc2", 75.00m);
            context.PermitTypes.AddRange(pt1, pt2);
            await context.SaveChangesAsync();

            var repository = new PermitTypeRepository(context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddAsync_ShouldAddPermitTypeToDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var permitType = new PermitType("New Type", "New Desc", 200.00m);
            var repository = new PermitTypeRepository(context);

            // Act
            await repository.AddAsync(permitType);
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.PermitTypes.FirstOrDefaultAsync(pt => pt.Id == permitType.Id);
            Assert.NotNull(saved);
            Assert.Equal("New Type", saved.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdatePermitTypeInDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var permitType = new PermitType("Original", "Desc", 100.00m);
            context.PermitTypes.Add(permitType);
            await context.SaveChangesAsync();

            var repository = new PermitTypeRepository(context);
            permitType.Deactivate(); // This changes IsActive to false

            // Act
            await repository.UpdateAsync(permitType);
            await context.SaveChangesAsync();

            // Assert
            var updated = await context.PermitTypes.FirstOrDefaultAsync(pt => pt.Id == permitType.Id);
            Assert.NotNull(updated);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemovePermitTypeFromDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var permitType = new PermitType("To Delete", "Desc", 100.00m);
            context.PermitTypes.Add(permitType);
            await context.SaveChangesAsync();

            var repository = new PermitTypeRepository(context);

            // Act
            await repository.DeleteAsync(permitType);
            await context.SaveChangesAsync();

            // Assert
            var deleted = await context.PermitTypes.FirstOrDefaultAsync(pt => pt.Id == permitType.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var permitType = new PermitType("Test", "Desc", 100.00m);
            context.PermitTypes.Add(permitType);
            await context.SaveChangesAsync();

            var repository = new PermitTypeRepository(context);

            // Act
            var result = await repository.ExistsAsync(permitType.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingId_ShouldReturnFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repository = new PermitTypeRepository(context);

            // Act
            var result = await repository.ExistsAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }
    }
}
