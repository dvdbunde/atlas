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

        private static List<AuditLog> BuildLogs(int count)
        {
            var logs = new List<AuditLog>();
            for (var i = 0; i < count; i++)
            {
                logs.Add(new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), $"Details {i}", "127.0.0.1"));
            }
            return logs;
        }

        [Fact]
        public async Task Handle_NoFilters_ShouldReturnAllAuditLogs()
        {
            var auditLogs = BuildLogs(2);
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.IsAny<AuditLogFilter>(), It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 2, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task Handle_WithUserIdFilter_ShouldPassFilterToRepository()
        {
            var userId = Guid.NewGuid();
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(userId, "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1")
            };
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.Is<AuditLogFilter>(f => f.UserId == userId), It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 1, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { UserId = userId };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(userId, result.Items[0].UserId);
        }

        [Fact]
        public async Task Handle_WithActionFilter_ShouldPassFilterToRepository()
        {
            var actionType = "Create";
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1")
            };
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.Is<AuditLogFilter>(f => f.Action == actionType), It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 1, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { Action = actionType };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(actionType, result.Items[0].Action);
        }

        [Fact]
        public async Task Handle_WithDateRangeFilter_ShouldPassFilterToRepository()
        {
            var dateFrom = DateTime.UtcNow.AddMinutes(-1);
            var dateTo = DateTime.UtcNow.AddMinutes(1);
            var auditLogs = BuildLogs(2);
            _mockRepository
                .Setup(r => r.GetPagedAsync(
                    It.Is<AuditLogFilter>(f => f.DateFrom == dateFrom && f.DateTo == dateTo),
                    It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 2, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { DateFrom = dateFrom, DateTo = dateTo };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task Handle_WithEntityIdFilter_ShouldPassFilterToRepository()
        {
            var recordId = Guid.NewGuid();
            var auditLogs = new List<AuditLog>
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", recordId, "Details 1", "127.0.0.1")
            };
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.Is<AuditLogFilter>(f => f.EntityId == recordId), It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 1, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { EntityId = recordId };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(recordId, result.Items[0].EntityId);
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldPassFilterToRepository()
        {
            var auditLogs = BuildLogs(1);
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.Is<AuditLogFilter>(f => f.SearchTerm == "Details"), It.IsAny<AuditLogSortOption>(), It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 1, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { SearchTerm = "Details" };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task Handle_WithPaging_ShouldPassPageToRepository()
        {
            var auditLogs = BuildLogs(5);
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.IsAny<AuditLogFilter>(), It.IsAny<AuditLogSortOption>(), It.Is<AuditLogPage>(p => p.PageNumber == 2 && p.PageSize == 5), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 25, PageNumber = 2, PageSize = 5 });

            var query = new GetAuditLogsQuery { PageNumber = 2, PageSize = 5 };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(25, result.TotalCount);
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(5, result.TotalPages);
        }

        [Fact]
        public async Task Handle_WithSortAscending_ShouldPassSortToRepository()
        {
            var auditLogs = BuildLogs(1);
            _mockRepository
                .Setup(r => r.GetPagedAsync(It.IsAny<AuditLogFilter>(), AuditLogSortOption.TimestampAsc, It.IsAny<AuditLogPage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedAuditLogResult { Items = auditLogs, TotalCount = 1, PageNumber = 1, PageSize = 20 });

            var query = new GetAuditLogsQuery { SortBy = AuditLogSortOptionDto.TimestampAsc };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new GetAuditLogsQueryHandler(null));
        }
    }
}

