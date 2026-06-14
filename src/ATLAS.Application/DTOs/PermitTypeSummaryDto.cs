using System;
using System.Collections.Generic;

namespace ATLAS.Application.DTOs
{
    public class PermitTypeSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public List<FieldDefinitionDto> Fields { get; set; } = new();
    }

    public class FieldDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
    }
}
