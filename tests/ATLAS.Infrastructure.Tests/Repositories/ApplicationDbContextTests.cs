using ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class ApplicationDbContextTests
    {
        [Fact]
        public void CanCreateDbContext_WithInMemoryDatabase()
        {
            // Arrange & Act
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            using var context = new ApplicationDbContext(options);
            
            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Applications);
            Assert.NotNull(context.PermitTypes);
            Assert.NotNull(context.Users);
            Assert.NotNull(context.AuditLogs);
        }

        [Fact]
        public async Task CanSaveAndRetrieveApplication()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test Notes");
            
            // Act - Save
            using (var context = new ApplicationDbContext(options))
            {
                context.Applications.Add(application);
                await context.SaveChangesAsync();
            }
            
            // Assert - Retrieve
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.Applications
                    .FirstOrDefaultAsync(a => a.Id == application.Id);
                Assert.NotNull(retrieved);
                Assert.Equal(application.Id, retrieved.Id);
                Assert.Equal("Test Notes", retrieved.CitizenNotes);
            }
        }
    }
}
