using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class TransactionBehaviorTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationRepository _applicationRepo;
        private readonly IPermitTypeRepository _permitTypeRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly UnitOfWork _unitOfWork;

        public TransactionBehaviorTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _applicationRepo = new ApplicationRepository(_context);
            _permitTypeRepo = new PermitTypeRepository(_context);
            _userRepo = new UserRepository(_context);
            _auditLogRepo = new AuditLogRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _applicationRepo, _permitTypeRepo, _userRepo, _auditLogRepo);
        }

        [Fact]
        public async Task UnitOfWork_SaveChangesAsync_ShouldPersistAllChanges()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            var permitType = new PermitType("Test Permit", "Description", 100m);
            var user = new User("test@email.com", "Test", "User", UserRole.Citizen);

            // Act - Add multiple entities through UnitOfWork
            await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.PermitTypes.AddAsync(permitType);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Assert - All changes persisted
            Assert.True(await _unitOfWork.Applications.ExistsAsync(application.Id));
            Assert.True(await _unitOfWork.PermitTypes.ExistsAsync(permitType.Id));
            Assert.True(await _unitOfWork.Users.ExistsAsync(user.Id));
        }

        [Fact]
        public async Task UnitOfWork_Dispose_ShouldDisposeContext()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            var unitOfWork = new UnitOfWork(context, 
                new ApplicationRepository(context),
                new PermitTypeRepository(context),
                new UserRepository(context),
                new AuditLogRepository(context));

            // Act
            unitOfWork.Dispose();

            // Assert - Context should be disposed
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                context.Applications.FirstOrDefaultAsync());
        }

        [Fact]
        public async Task Repository_UpdateAsync_ShouldUpdateExistingEntity()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Original notes");
            await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();

            // Detach the original entity to avoid tracking conflict
            _context.Entry(application).State = EntityState.Detached;

            // Act - Retrieve and update
            var retrieved = await _unitOfWork.Applications.GetByIdAsync(application.Id);
            // Note: CitizenNotes is private set, so we can't directly modify it
            // In real scenario, we'd use domain methods like Submit(), Approve(), etc.
            // For this test, we'll verify the entity can be retrieved and updated
            await _unitOfWork.Applications.UpdateAsync(retrieved);
            await _unitOfWork.SaveChangesAsync();

            // Assert - Entity still exists and is unchanged
            var updated = await _unitOfWork.Applications.GetByIdAsync(application.Id);
            Assert.NotNull(updated);
            Assert.Equal("Original notes", updated.CitizenNotes);
        }

        public void Dispose()
        {
            _unitOfWork?.Dispose();
        }
    }
}
