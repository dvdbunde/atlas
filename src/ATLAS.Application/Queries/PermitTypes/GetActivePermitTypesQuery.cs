using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ATLAS.Application.Queries.PermitTypes
{
    public class GetActivePermitTypesQuery : IRequest<IEnumerable<PermitTypeSummaryDto>>
    {
        // No parameters - returns all active permit types
    }

    public class GetActivePermitTypesQueryHandler : IRequestHandler<GetActivePermitTypesQuery, IEnumerable<PermitTypeSummaryDto>>
    {
        private readonly IApplicationRepository _repository;
        private readonly ILogger<GetActivePermitTypesQueryHandler> _logger;

        public GetActivePermitTypesQueryHandler(
            IApplicationRepository repository,
            ILogger<GetActivePermitTypesQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PermitTypeSummaryDto>> Handle(GetActivePermitTypesQuery request, CancellationToken cancellationToken)
        {
            var permitTypes = await _repository.GetActivePermitTypesAsync(cancellationToken);

            var dtos = new List<PermitTypeSummaryDto>();
            foreach (var pt in permitTypes)
            {
                dtos.Add(new PermitTypeSummaryDto
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Description = pt.Description,
                    Fee = pt.Fee,
                    Fields = pt.Fields.Select(f => new FieldDefinitionDto
                    {
                        Name = f.Name,
                        Type = f.Type.ToString(),
                        IsRequired = f.IsRequired,
                        DefaultValue = f.DefaultValue
                    }).ToList()
                });
            }

            _logger.LogInformation("Retrieved {Count} active permit types", dtos.Count);

            return dtos;
        }
    }
}
