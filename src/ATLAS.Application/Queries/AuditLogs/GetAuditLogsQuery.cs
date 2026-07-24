using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.AuditLogs
{
    public enum AuditLogSortOptionDto
    {
        TimestampDesc,
        TimestampAsc
    }

    /// <summary>
    /// Query to retrieve a paged, filtered, sorted, searchable list of audit logs.
    /// Audit logs are an append-only dataset; paging/sorting/filtering are delegated
    /// to the repository so the full history is never loaded into memory.
    /// </summary>
    public class GetAuditLogsQuery : IRequest<AuditLogListResult>
    {
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public Guid? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? SearchTerm { get; set; }
        public AuditLogSortOptionDto SortBy { get; set; } = AuditLogSortOptionDto.TimestampDesc;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>Paged result wrapper for the audit log list.</summary>
    public class AuditLogListResult
    {
        public IReadOnlyList<AuditLogDto> Items { get; init; } = Array.Empty<AuditLogDto>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, AuditLogListResult>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogsQueryHandler(IAuditLogRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<AuditLogListResult> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var filter = new AuditLogFilter
            {
                UserId = request.UserId,
                Action = request.Action,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                EntityId = request.EntityId,
                EntityType = request.EntityType,
                SearchTerm = request.SearchTerm
            };

            var sort = request.SortBy == AuditLogSortOptionDto.TimestampAsc
                ? AuditLogSortOption.TimestampAsc
                : AuditLogSortOption.TimestampDesc;

            var page = new AuditLogPage
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _repository.GetPagedAsync(filter, sort, page, cancellationToken);

            var dtos = result.Items.Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress
            }).ToList();

            return new AuditLogListResult
            {
                Items = dtos,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}

