using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var user = new User("test@email.com", "John", "Doe", UserRole.Citizen);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@email.com", result.Email);
            Assert.Equal(UserRole.Citizen, result.Role);
        }

        [Fact]
        public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
        {
            // Arrange
            var user = new User("unique@email.com", "Jane", "Doe", UserRole.Officer);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync("unique@email.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jane", result.FirstName);
        }

        [Fact]
        public async Task GetByRoleAsync_WithMixedRoles_ShouldReturnOnlyMatchingRole()
        {
            // Arrange
            var citizen1 = new User("citizen1@test.com", "C1", "Test", UserRole.Citizen);
            var citizen2 = new User("citizen2@test.com", "C2", "Test", UserRole.Citizen);
            var officer = new User("officer@test.com", "Off", "Test", UserRole.Officer);
            
            _context.Users.AddRange(citizen1, citizen2, officer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByRoleAsync(UserRole.Citizen);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, u => Assert.Equal(UserRole.Citizen, u.Role));
        }

        [Fact]
        public async Task ExistsAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            var user = new User("exists@test.com", "Test", "User", UserRole.Citizen);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _repository.ExistsAsync(user.Id);

            // Assert
            Assert.True(exists);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
