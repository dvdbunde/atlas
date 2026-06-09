using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class DocumentUploadedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly DocumentUploadedEventHandler _handler;

        public DocumentUploadedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _handler = new DocumentUploadedEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();
            var fileName = "test.pdf";            
            var uploadedById = Guid.NewGuid();    
            var evt = new DocumentUploadedEvent(documentId, applicationId, uploadedById, fileName);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            // Note: DocumentUploadedEventHandler logs to "Document" entity type with documentId
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Document", documentId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("DocumentUploaded", log.Action);
            Assert.Equal("Document", log.EntityType);
            Assert.Equal(documentId, log.EntityId);
            Assert.Contains(fileName, log.Details);
            Assert.Contains(uploadedById.ToString(), log.Details);
            Assert.Contains(applicationId.ToString(), log.Details); // Details mention the application
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DocumentUploadedEventHandler(null!));
        }
    }
}
