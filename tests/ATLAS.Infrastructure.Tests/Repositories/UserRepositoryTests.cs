using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "test@example.com", "John", "Doe", UserRole.Citizen);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "unique@email.com", "Jane", "Doe", UserRole.Officer);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByEmailAsync("unique@email.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unique@email.com", result.Email);
        }

        [Fact]
        public async Task GetByRoleAsync_WithExistingRole_ShouldReturnUsers()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var citizen = new User(Guid.NewGuid(), "citizen@test.com", "Citizen", "One", UserRole.Citizen);
            var officer = new User(Guid.NewGuid(), "officer@test.com", "Officer", "One", UserRole.Officer);
            context.Users.AddRange(citizen, officer);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByRoleAsync(UserRole.Citizen);

            // Assert
            Assert.Single(result);
            Assert.Equal(UserRole.Citizen, result.First().Role);
        }

        [Fact]
        public async Task AddAsync_ShouldAddUserToDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "new@user.com", "New", "User", UserRole.Citizen);
            var repository = new UserRepository(context);

            // Act
            await repository.AddAsync(user);
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(saved);
            Assert.Equal("new@user.com", saved.Email);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUserInDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "update@test.com", "Update", "Me", UserRole.Citizen);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);
            user.RecordLogin(); // Verify update persists state changes

            // Act
            await repository.UpdateAsync(user);
            await context.SaveChangesAsync();

            // Assert
            var updated = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(updated);
            Assert.NotNull(updated.LastLoginDate);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenUserPresent()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "exists@test.com", "Exists", "Me", UserRole.Citizen);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var exists = await repository.ExistsAsync(user.Id);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var user = new User(Guid.NewGuid(), "exists@test.com", "Exists", "User", UserRole.Citizen);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new UserRepository(context);

            // Act
            var result = await repository.ExistsAsync(user.Id);

            // Assert
            Assert.True(result);
        }
    }
}
