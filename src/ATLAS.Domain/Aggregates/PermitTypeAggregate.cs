using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Domain.Aggregates
{
    public class PermitTypeAggregate
    {
        private readonly Entities.PermitType _permitType;
        private readonly List<PermitField> _fields;
        private readonly List<DocumentRequirement> _documentRequirements;

        public Entities.PermitType PermitType => _permitType;
        public IReadOnlyList<PermitField> Fields => _fields.AsReadOnly();
        public IReadOnlyList<DocumentRequirement> DocumentRequirements => _documentRequirements.AsReadOnly();

        public PermitTypeAggregate(Entities.PermitType permitType)
        {
            _permitType = permitType ?? throw new ArgumentNullException(nameof(permitType));
            _fields = new List<PermitField>();
            _documentRequirements = new List<DocumentRequirement>();
        }

        public void AddField(PermitField field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            
            if (_fields.Any(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Field '{field.Name}' already exists");

            _fields.Add(field);
        }

        public void AddDocumentRequirement(DocumentRequirement requirement)
        {
            if (requirement == null)
                throw new ArgumentNullException(nameof(requirement));
            
            if (_documentRequirements.Any(d => d.DocumentType.Equals(requirement.DocumentType, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Document requirement '{requirement.DocumentType}' already exists");

            _documentRequirements.Add(requirement);
        }

        public void ValidateInvariants()
        {
            var duplicateFields = _fields
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateFields.Any())
                throw new DomainException($"Duplicate field names found: {string.Join(", ", duplicateFields)}");

            var duplicateDocs = _documentRequirements
                .GroupBy(d => d.DocumentType, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateDocs.Any())
                throw new DomainException($"Duplicate document types found: {string.Join(", ", duplicateDocs)}");
        }
    }
}
