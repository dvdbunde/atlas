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
        private readonly IPermitTypeRepository _repository;
        private readonly ILogger<GetActivePermitTypesQueryHandler> _logger;

        public GetActivePermitTypesQueryHandler(
            IPermitTypeRepository repository,
            ILogger<GetActivePermitTypesQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PermitTypeSummaryDto>> Handle(GetActivePermitTypesQuery request, CancellationToken cancellationToken)
        {
            var permitTypes = await _repository.GetAllActiveAsync(cancellationToken);

            var dtos = new List<PermitTypeSummaryDto>();
            foreach (var pt in permitTypes)
            {
                dtos.Add(new PermitTypeSummaryDto
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Description = pt.Description,
                    Fee = pt.Fee,
                    IsActive = pt.IsActive                    
                });
            }

            _logger.LogInformation("Retrieved {Count} active permit types", dtos.Count);

            return dtos;
        }
    }
}