using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class DomainEventToAuditLogPersistenceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly ApplicationApprovedEventHandler _approvedHandler;
        private readonly ApplicationSubmittedEventHandler _submittedHandler;
        private readonly ApplicationRejectedEventHandler _rejectedHandler;
        private readonly ApplicationInfoRequestedEventHandler _infoRequestedHandler;
        private readonly ApplicationResubmittedEventHandler _resubmittedHandler;
        private readonly ApplicationUnderReviewEventHandler _underReviewHandler;
        private readonly DocumentUploadedEventHandler _documentUploadedHandler;
        private readonly PermitTypeActivatedEventHandler _activatedHandler;
        private readonly PermitTypeDeactivatedEventHandler _deactivatedHandler;
        private readonly PermitTypeFieldAddedEventHandler _fieldAddedHandler;
        private readonly UserRoleChangedEventHandler _roleChangedHandler;
        private readonly ApplicationAssignedToOfficerEventHandler _assignedHandler;

        public DomainEventToAuditLogPersistenceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            
            _approvedHandler = new ApplicationApprovedEventHandler(_auditLogRepository);
            _submittedHandler = new ApplicationSubmittedEventHandler(_auditLogRepository);
            _rejectedHandler = new ApplicationRejectedEventHandler(_auditLogRepository);
            _infoRequestedHandler = new ApplicationInfoRequestedEventHandler(_auditLogRepository);
            _resubmittedHandler = new ApplicationResubmittedEventHandler(_auditLogRepository);
            _underReviewHandler = new ApplicationUnderReviewEventHandler(_auditLogRepository);
            _documentUploadedHandler = new DocumentUploadedEventHandler(_auditLogRepository);
            _activatedHandler = new PermitTypeActivatedEventHandler(_auditLogRepository);
            _deactivatedHandler = new PermitTypeDeactivatedEventHandler(_auditLogRepository);
            _fieldAddedHandler = new PermitTypeFieldAddedEventHandler(_auditLogRepository);
            _roleChangedHandler = new UserRoleChangedEventHandler(_auditLogRepository);
            _assignedHandler = new ApplicationAssignedToOfficerEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task ApplicationApprovedEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var evt = new ApplicationApprovedEvent(Guid.NewGuid(), Guid.NewGuid());

            // Act
            await _approvedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationApproved", log.Action);
            Assert.Equal("Application", log.EntityType);
            Assert.Equal(evt.ApplicationId, log.EntityId);
            Assert.Contains(evt.OfficerId.ToString(), log.Details);
        }

        [Fact]
        public async Task ApplicationSubmittedEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var evt = new ApplicationSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            // Act
            await _submittedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationSubmitted", log.Action);
            Assert.Contains(evt.CitizenId.ToString(), log.Details);
        }

        [Fact]
        public async Task ApplicationRejectedEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var evt = new ApplicationRejectedEvent(Guid.NewGuid(), Guid.NewGuid(), "INVALID_DOCUMENT");

            // Act
            await _rejectedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationRejected", log.Action);
            Assert.Contains(evt.ReasonCode, log.Details);
        }

        [Fact]
        public async Task DocumentUploadedEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var evt = new DocumentUploadedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test.pdf");

            // Act
            await _documentUploadedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Document", evt.DocumentId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("DocumentUploaded", log.Action);
            Assert.Contains("test.pdf", log.Details);
        }

        [Fact]
        public async Task UserRoleChangedEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var evt = new UserRoleChangedEvent(Guid.NewGuid(), UserRole.Citizen, UserRole.Officer);

            // Act
            await _roleChangedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("User", evt.UserId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("UserRoleChanged", log.Action);
            Assert.Contains("Citizen", log.Details);
            Assert.Contains("Officer", log.Details);
        }

        [Fact]
        public async Task MultipleEvents_ShouldCreateMultipleAuditLogs()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();

            // Act - Submit and Approve
            await _submittedHandler.Handle(new ApplicationSubmittedEvent(applicationId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
            await _underReviewHandler.Handle(new ApplicationUnderReviewEvent(applicationId, officerId), CancellationToken.None);
            await _approvedHandler.Handle(new ApplicationApprovedEvent(applicationId, officerId), CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", applicationId);
            Assert.Equal(3, auditLogs.Count());
            
            var actions = auditLogs.Select(al => al.Action).ToList();
            Assert.Contains("ApplicationSubmitted", actions);
            Assert.Contains("ApplicationUnderReview", actions);
            Assert.Contains("ApplicationApproved", actions);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
