using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// Get audit logs (F-20)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
            [FromQuery] Guid? userId,
            [FromQuery] string? actionType,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] Guid? recordId,
            CancellationToken cancellationToken)
        {
            var query = new GetAuditLogsQuery
            {
                UserId = userId,
                ActionType = actionType,
                DateFrom = dateFrom,
                DateTo = dateTo,
                RecordId = recordId
            };
            var results = await _mediator.Send(query, cancellationToken);
            return Ok(results);
        }

        /// <summary>
        /// Export audit logs to CSV (F-23)
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] Guid? userId,
            [FromQuery] string? actionType,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            // TODO: Implement CSV export
            // 1. Get audit logs using GetAuditLogsQuery
            // 2. Convert to CSV format
            // 3. Return as FileResult
            throw new NotImplementedException("CSV export not yet implemented");
        }
    }
}