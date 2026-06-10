using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetAuditLogsQuery : IRequest<IEnumerable<AuditLogDto>>
    {
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public Guid? EntityId { get; set; }
    }

    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, IEnumerable<AuditLogDto>>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogsQueryHandler(IAuditLogRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            // Start with base query
            var auditLogs = await _repository.GetAllAsync(cancellationToken);
            
            // Apply filters based on request parameters
            if (request.UserId.HasValue)
                auditLogs = auditLogs.Where(a => a.UserId == request.UserId).ToList();
            
            if (!string.IsNullOrEmpty(request.Action))
                auditLogs = auditLogs.Where(a => a.Action == request.Action).ToList();
            
            if (request.DateFrom.HasValue)
                auditLogs = auditLogs.Where(a => a.Timestamp >= request.DateFrom).ToList();
            
            if (request.DateTo.HasValue)
                auditLogs = auditLogs.Where(a => a.Timestamp <= request.DateTo).ToList();
            
            if (request.EntityId.HasValue)
                auditLogs = auditLogs.Where(a => a.EntityId == request.EntityId).ToList();
            
            // Map to AuditLogDto
            return auditLogs.Select(a => new AuditLogDto
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
        }
    }
}
