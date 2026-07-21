//----------------------
// AuditLogs Controller Adapter
// Implements IAuditLogsController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Queries.AuditLogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public sealed class AuditLogsController : AuditLogsControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public override async Task<ActionResult<PagedAuditLogResponse>> GetAuditLogs(
            Guid? userId = null,
            string? action = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null,
            Guid? entityId = null,
            string? searchTerm = null,
            AuditLogSortOption? sort = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var sortOption = Enum.TryParse<AuditLogSortOptionDto>(sort?.ToString(), ignoreCase: true, out var parsed)
                ? parsed
                : AuditLogSortOptionDto.TimestampDesc;

            var query = new GetAuditLogsQuery
            {
                UserId = userId,
                Action = action,
                DateFrom = dateFrom?.DateTime,
                DateTo = dateTo?.DateTime,
                EntityId = entityId,
                SearchTerm = searchTerm,
                SortBy = sortOption,
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var result = await _mediator.Send(query, default);

            var response = new PagedAuditLogResponse
            {
                Items = result.Items.Select(dto => dto.ToResponse()).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }

        public override async Task<ActionResult<AuditLogResponse>> GetAuditLogById(Guid id)
        {
            var result = await _mediator.Send(new GetAuditLogDetailQuery { Id = id }, default);
            if (result is null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public override async Task<ActionResult<string>> ExportAuditLogs(
            Guid? userId = null,
            string? action = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null)
        {
            // TODO: Implement CSV export
            // Return 501 Not Implemented
            return StatusCode(501);
        }
    }
}
