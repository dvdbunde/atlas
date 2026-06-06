using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetPermitTypesQuery : IRequest<IEnumerable<PermitTypeDto>>
    {
        public bool IncludeInactive { get; set; } = false;
    }

    public class GetPermitTypesQueryHandler : IRequestHandler<GetPermitTypesQuery, IEnumerable<PermitTypeDto>>
    {
        private readonly IPermitTypeRepository _repository;

        public GetPermitTypesQueryHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<PermitTypeDto>> Handle(GetPermitTypesQuery request, CancellationToken cancellationToken)
        {
            var permitTypes = await _repository.GetAllAsync(cancellationToken);
            
            // Filter out inactive if not requested
            if (!request.IncludeInactive)
                permitTypes = permitTypes.Where(pt => pt.IsActive).ToList();
            
            // Map to PermitTypeDto
            var dtos = permitTypes.Select(pt => new PermitTypeDto
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

    public class GetPermitTypeByIdQuery : IRequest<PermitTypeDto?>
    {
        public Guid PermitTypeId { get; set; }
    }

    public class GetPermitTypeByIdQueryHandler : IRequestHandler<GetPermitTypeByIdQuery, PermitTypeDto?>
    {
        private readonly IPermitTypeRepository _repository;

        public GetPermitTypeByIdQueryHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<PermitTypeDto?> Handle(GetPermitTypeByIdQuery request, CancellationToken cancellationToken)
        {
            var permitType = await _repository.GetByIdAsync(request.PermitTypeId, cancellationToken);
            
            if (permitType == null)
                return null;

            return new PermitTypeDto
            {
                Id = permitType.Id,
                Name = permitType.Name,
                Description = permitType.Description,
                Fee = permitType.Fee,
                IsActive = permitType.IsActive
            };
        }
    }
}
