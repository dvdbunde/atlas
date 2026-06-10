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

        public override async Task<ActionResult<ICollection<AuditLogResponse>>> Auditlogs(
            Guid? userId = null,
            string? action = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null,
            Guid? entityId = null)
        {
            var query = new GetAuditLogsQuery
            {
                UserId = userId,
                Action = action,
                DateFrom = dateFrom?.DateTime,
                DateTo = dateTo?.DateTime,
                EntityId = entityId
            };
            var results = await _mediator.Send(query, default);
            var response = new List<AuditLogResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return Ok(response);
        }

        public override async Task<ActionResult<string>> Export(
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