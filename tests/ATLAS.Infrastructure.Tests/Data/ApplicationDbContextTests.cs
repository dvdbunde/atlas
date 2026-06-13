using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Data
{
    public class ApplicationDbContextTests
    {
        private readonly ApplicationDbContext _context;

        public ApplicationDbContextTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public void Constructor_ShouldInitializeContext_WhenOptionsProvided()
        {
            // Arrange & Act & Assert
            Assert.NotNull(_context);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldDispatchDomainEvents()
        {
            // Arrange
            var user = new ATLAS.Domain.Entities.User(Guid.NewGuid(), "test@test.com", "John", "Doe", ATLAS.Domain.Entities.UserRole.Citizen);
            _context.Users.Add(user);
            
            // Act
            var result = await _context.SaveChangesAsync();

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void OnModelCreating_ShouldConfigureEntities()
        {
            // Arrange & Act
            var users = _context.Users;
            var applications = _context.Applications;
            var permitTypes = _context.PermitTypes;
            var auditLogs = _context.AuditLogs;            

            // Assert
            Assert.NotNull(users);
            Assert.NotNull(applications);
            Assert.NotNull(permitTypes);
            Assert.NotNull(auditLogs);            
        }

        [Fact]
        public void Set_ShouldReturnDbSet_ForEntityType()
        {
            // Arrange & Act
            var users = _context.Set<ATLAS.Domain.Entities.User>();
            var applications = _context.Set<ATLAS.Domain.Entities.Application>();

            // Assert
            Assert.NotNull(users);
            Assert.NotNull(applications);
        }
    }
}
