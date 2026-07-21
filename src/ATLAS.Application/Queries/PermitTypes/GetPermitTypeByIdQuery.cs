using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.Enums;

namespace ATLAS.Application.Queries.PermitTypes
{  
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
                IsActive = permitType.IsActive,
                Fields = permitType.Fields.Select(f => new FieldDefinitionDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Type = f.Type,
                        IsRequired = f.IsRequired,
                        DefaultValue = f.DefaultValue,
                        Options = f.Options.ToList()
                    })
                    .ToList(),
                DocumentRequirements = permitType.DocumentRequirements.Select(r => new FieldDefinitionDto
                    {
                        Id = r.Id,
                        Name = r.DocumentType,
                        Type = FieldType.FileUpload,
                        IsRequired = r.IsRequired,
                        AllowedExtensions = string.Join(",", r.AllowedExtensions),
                        MaxFileSizeBytes = r.MaxFileSizeBytes
                    })
                    .ToList()
            };
        }
    }
}
