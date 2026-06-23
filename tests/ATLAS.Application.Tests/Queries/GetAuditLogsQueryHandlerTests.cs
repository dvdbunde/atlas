using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetAuditLogsQueryHandlerTests
    {
        private readonly Mock<IAuditLogRepository> _mockRepository;
        private readonly GetAuditLogsQueryHandler _handler;

        public GetAuditLogsQueryHandlerTests()
        {
            _mockRepository = new Mock<IAuditLogRepository>();
            _handler = new GetAuditLogsQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_NoFilters_ShouldReturnAllAuditLogs()
        {
            // Arrange
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1")
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogs);

            var query = new GetAuditLogsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Handle_WithUserIdFilter_ShouldReturnFilteredLogs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(userId, "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1")
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogs);

            var query = new GetAuditLogsQuery { UserId = userId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, log => Assert.Equal(userId, log.UserId));
        }

        [Fact]
        public async Task Handle_WithActionTypeFilter_ShouldReturnFilteredLogs()
        {
            // Arrange
            var actionType = "Create";
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1")
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogs);

            var query = new GetAuditLogsQuery { Action = actionType };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, log => Assert.Equal(actionType, log.Action));
        }

        [Fact]
        public async Task Handle_WithDateRangeFilter_ShouldReturnFilteredLogs()
        {
            // Arrange
            // Use a wide date range to ensure logs created with DateTime.UtcNow are captured
            var dateFrom = DateTime.UtcNow.AddMinutes(-1);
            var dateTo = DateTime.UtcNow.AddMinutes(1);
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1")
            };
            // Timestamps are set to DateTime.UtcNow in constructor, which should be within our range

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogs);

            var query = new GetAuditLogsQuery { DateFrom = dateFrom, DateTo = dateTo };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Handle_WithRecordIdFilter_ShouldReturnFilteredLogs()
        {
            // Arrange
            var recordId = Guid.NewGuid();
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", recordId, "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1")
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditLogs);

            var query = new GetAuditLogsQuery { EntityId = recordId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, log => Assert.Equal(recordId, log.EntityId));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GetAuditLogsQueryHandler(null));
        }
    }
}
