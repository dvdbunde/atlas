using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.PermitTypes
{
    public class GetPermitTypesQuery : IRequest<IEnumerable<PermitTypeSummaryDto>>
    {
        public bool IncludeInactive { get; set; } = false;
    }

    public class GetPermitTypesQueryHandler : IRequestHandler<GetPermitTypesQuery, IEnumerable<PermitTypeSummaryDto>>
    {
        private readonly IPermitTypeRepository _repository;

        public GetPermitTypesQueryHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<PermitTypeSummaryDto>> Handle(GetPermitTypesQuery request, CancellationToken cancellationToken)
        {
            var permitTypes = await _repository.GetAllAsync(cancellationToken);
            
            // Filter out inactive if not requested
            if (!request.IncludeInactive)
                permitTypes = permitTypes.Where(pt => pt.IsActive).ToList();
            
            // Map to PermitTypeSummaryDto
            var dtos = permitTypes.Select(pt => new PermitTypeSummaryDto
            {
                Id = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                Fee = pt.Fee,
                IsActive = pt.IsActive
            }).ToList();
            
            return dtos;
        }
    }
}
