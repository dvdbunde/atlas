using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class DomainEventToAuditLogPersistenceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly Mock<ICurrentUserService> _currentUserService;
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
        private readonly ApplicationAssignedToOfficerEventHandler _assignedHandler;

        public DomainEventToAuditLogPersistenceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService = new Mock<ICurrentUserService>();
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

            _approvedHandler = new ApplicationApprovedEventHandler(_auditLogRepository, _currentUserService.Object);
            _submittedHandler = new ApplicationSubmittedEventHandler(_auditLogRepository, _currentUserService.Object);
            _rejectedHandler = new ApplicationRejectedEventHandler(_auditLogRepository, _currentUserService.Object);
            _infoRequestedHandler = new ApplicationInfoRequestedEventHandler(_auditLogRepository, _currentUserService.Object);
            _resubmittedHandler = new ApplicationResubmittedEventHandler(_auditLogRepository, _currentUserService.Object);
            _underReviewHandler = new ApplicationUnderReviewEventHandler(_auditLogRepository, _currentUserService.Object);
            _documentUploadedHandler = new DocumentUploadedEventHandler(_auditLogRepository, _currentUserService.Object);
            _activatedHandler = new PermitTypeActivatedEventHandler(_auditLogRepository, _currentUserService.Object);
            _deactivatedHandler = new PermitTypeDeactivatedEventHandler(_auditLogRepository, _currentUserService.Object);
            _assignedHandler = new ApplicationAssignedToOfficerEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task ApplicationApprovedEvent_ShouldPersistToAuditLog()
        {
            var evt = new ApplicationApprovedEvent(Guid.NewGuid());

            await _approvedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationApproved", log.Action);
            Assert.Equal("Application", log.EntityType);
            Assert.Equal(evt.ApplicationId, log.EntityId);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public async Task ApplicationSubmittedEvent_ShouldPersistToAuditLog()
        {
            var evt = new ApplicationSubmittedEvent(Guid.NewGuid(), Guid.NewGuid());

            await _submittedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationSubmitted", log.Action);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public async Task ApplicationRejectedEvent_ShouldPersistToAuditLog()
        {
            var evt = new ApplicationRejectedEvent(Guid.NewGuid(), "INVALID_DOCUMENT");

            await _rejectedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", evt.ApplicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationRejected", log.Action);
            Assert.Contains(evt.ReasonCode, log.Details);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public async Task DocumentUploadedEvent_ShouldPersistToAuditLog()
        {
            var evt = new DocumentUploadedEvent(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");

            await _documentUploadedHandler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            var auditLogs = await _auditLogRepository.GetByEntityAsync("Document", evt.DocumentId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("DocumentUploaded", log.Action);
            Assert.Contains("test.pdf", log.Details);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public async Task MultipleEvents_ShouldCreateMultipleAuditLogs()
        {
            var applicationId = Guid.NewGuid();

            await _submittedHandler.Handle(new ApplicationSubmittedEvent(applicationId, Guid.NewGuid()), CancellationToken.None);
            await _underReviewHandler.Handle(new ApplicationUnderReviewEvent(applicationId), CancellationToken.None);
            await _approvedHandler.Handle(new ApplicationApprovedEvent(applicationId), CancellationToken.None);
            await _context.SaveChangesAsync();

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
