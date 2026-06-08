//----------------------
// AuditLogs Controller Adapter
// Implements IAuditLogsController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ATLAS.API.Controllers.Generated
{
    public partial class AuditLogsController : ControllerBase, IAuditLogsController
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _implementation = this;
        }

        public async Task<ActionResult<ICollection<AuditLogResponse>>> AuditlogsAsync(
            Guid? userId = null,
            string? actionType = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null,
            Guid? recordId = null)
        {
            var query = new GetAuditLogsQuery
            {
                UserId = userId,
                ActionType = actionType,
                DateFrom = dateFrom?.DateTime,
                DateTo = dateTo?.DateTime,
                RecordId = recordId
            };
            var results = await _mediator.Send(query, default);
            var response = new List<AuditLogResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return Ok(response);
        }

        public async Task<ActionResult<string>> ExportAsync(
            Guid? userId = null,
            string? actionType = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null)
        {
            // TODO: Implement CSV export
            // Return 501 Not Implemented
            return StatusCode(501);
        }
    }
}