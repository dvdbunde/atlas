using MediatR;
using System;
using System.Linq;
using System.Reflection;
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
    public class GetAuditLogDetailQueryHandlerTests
    {
        private readonly Mock<IAuditLogRepository> _mockRepository;
        private readonly GetAuditLogDetailQueryHandler _handler;

        public GetAuditLogDetailQueryHandlerTests()
        {
            _mockRepository = new Mock<IAuditLogRepository>();
            _handler = new GetAuditLogDetailQueryHandler(_mockRepository.Object);
        }

        private static AuditLog BuildLog(Guid id, Guid? userId = null)
        {
            var log = new AuditLog(userId, "Create", "Application", Guid.NewGuid(), "Details", "127.0.0.1");
            typeof(Entity<Guid>)
                .GetProperty(nameof(Entity<Guid>.Id))!
                .SetValue(log, id);
            return log;
        }

        [Fact]
        public async Task Handle_ExistingEntry_ShouldReturnMappedDto()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var log = BuildLog(id, userId);
            _mockRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(log);

            var query = new GetAuditLogDetailQuery { Id = id };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("Create", result.Action);
            Assert.Equal("Application", result.EntityType);
            Assert.Equal(log.EntityId, result.EntityId);
            Assert.Equal("Details", result.Details);
            Assert.Equal(log.Timestamp, result.Timestamp);
            Assert.Equal("127.0.0.1", result.IpAddress);
        }

        [Fact]
        public async Task Handle_MissingEntry_ShouldReturnNull()
        {
            var id = Guid.NewGuid();
            _mockRepository
                .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AuditLog?)null);

            var query = new GetAuditLogDetailQuery { Id = id };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoryWithCorrectIdAndToken()
        {
            var id = Guid.NewGuid();
            var token = new CancellationToken();
            _mockRepository
                .Setup(r => r.GetByIdAsync(id, token))
                .ReturnsAsync((AuditLog?)null);

            var query = new GetAuditLogDetailQuery { Id = id };

            await _handler.Handle(query, token);

            _mockRepository.Verify(r => r.GetByIdAsync(id, token), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new GetAuditLogDetailQueryHandler(null));
        }
    }
}
