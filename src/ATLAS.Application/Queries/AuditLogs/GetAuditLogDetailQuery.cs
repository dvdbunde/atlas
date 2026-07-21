using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.AuditLogs
{
    /// <summary>
    /// Query to retrieve a single audit log entry by id (read-only).
    /// Returns null when no entry exists with the given id.
    /// </summary>
    public class GetAuditLogDetailQuery : IRequest<AuditLogDto?>
    {
        public Guid Id { get; set; }
    }

    public class GetAuditLogDetailQueryHandler : IRequestHandler<GetAuditLogDetailQuery, AuditLogDto?>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogDetailQueryHandler(IAuditLogRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<AuditLogDto?> Handle(GetAuditLogDetailQuery request, CancellationToken cancellationToken)
        {
            var log = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (log is null)
                return null;

            return new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Details = log.Details,
                Timestamp = log.Timestamp,
                IpAddress = log.IpAddress
            };
        }
    }
}
