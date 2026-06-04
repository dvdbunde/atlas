using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class ApplicationRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingApplication_ShouldReturnApplication()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test Notes");
            context.Applications.Add(application);
            await context.SaveChangesAsync();

            var repository = new ApplicationRepository(context);

            // Act
            var result = await repository.GetByIdAsync(application.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repository = new ApplicationRepository(context);

            // Act
            var result = await repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_ShouldAddApplicationToDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "New Application");
            var repository = new ApplicationRepository(context);

            // Act
            await repository.AddAsync(application);
            await context.SaveChangesAsync();

            // Assert
            var savedApplication = await context.Applications.FirstOrDefaultAsync(a => a.Id == application.Id);
            Assert.NotNull(savedApplication);
            Assert.Equal("New Application", savedApplication.CitizenNotes);
        }
    }
}
