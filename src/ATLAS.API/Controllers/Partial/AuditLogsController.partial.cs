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
    public partial class AuditLogsController : ControllerBase, IAuditLogsController
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<ICollection<AuditLogResponse>> AuditlogsAsync(
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
            return response;
        }

        public async Task<string> ExportAsync(
            Guid? userId = null,
            string? actionType = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null)
        {
            // TODO: Implement CSV export
            throw new System.NotImplementedException("CSV export not yet implemented");
        }
    }
}
