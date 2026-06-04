using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class PermitTypeRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PermitTypeRepository _repository;

        public PermitTypeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new PermitTypeRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingPermitType_ShouldReturnPermitType()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Construction permit", 150.00m);
            _context.PermitTypes.Add(permitType);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(permitType.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Building Permit", result.Name);
            Assert.Equal(150.00m, result.Fee);
        }

        [Fact]
        public async Task GetAllActiveAsync_WithMixedPermitTypes_ShouldReturnOnlyActive()
        {
            // Arrange
            var active1 = new PermitType("Active1", "Description", 100m);
            var active2 = new PermitType("Active2", "Description", 200m);
            var inactive = new PermitType("Inactive", "Description", 300m);
            
            // Use Deactivate() method to change IsActive
            inactive.Deactivate(Guid.NewGuid());
            
            _context.PermitTypes.AddRange(active1, active2, inactive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllActiveAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, pt => Assert.True(pt.IsActive));
        }

        [Fact]
        public async Task AddAsync_ShouldAddPermitTypeToDatabase()
        {
            // Arrange
            var permitType = new PermitType("New Permit", "Description", 99.99m);
            await _repository.AddAsync(permitType);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.PermitTypes.FirstOrDefaultAsync(pt => pt.Id == permitType.Id);
            Assert.NotNull(saved);
            Assert.Equal("New Permit", saved.Name);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdatePermitTypeInDatabase()
        {
            // Arrange
            var permitType = new PermitType("Original", "Description", 100m);
            _context.PermitTypes.Add(permitType);
            await _context.SaveChangesAsync();

            // Act - Retrieve and deactivate using domain method
            var retrieved = await _repository.GetByIdAsync(permitType.Id);
            retrieved.Deactivate(Guid.NewGuid());
            await _repository.UpdateAsync(retrieved);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _context.PermitTypes.FirstOrDefaultAsync(pt => pt.Id == permitType.Id);
            Assert.False(updated.IsActive);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
