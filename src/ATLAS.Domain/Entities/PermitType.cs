using System;
using System.Collections.Generic;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Entities
{
    public class PermitType : Entity<Guid>
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public decimal Fee { get; private set; }
        
        private readonly List<PermitField> _fields = new();
        public IReadOnlyList<PermitField> Fields => _fields.AsReadOnly();
        
        private readonly List<DocumentRequirement> _documentRequirements = new();
        public IReadOnlyList<DocumentRequirement> DocumentRequirements => _documentRequirements.AsReadOnly();

        public PermitType(string name, string description, decimal fee)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            
            if (name.Length < 3 || name.Length > 100)
                throw new ArgumentException("Name must be between 3 and 100 characters", nameof(name));
            
            if (fee < 0)
                throw new ArgumentException("Fee cannot be negative", nameof(fee));

            Id = Guid.NewGuid();
            Name = name;
            Description = description ?? string.Empty;
            IsActive = true;
            Fee = fee;
        }

        protected PermitType()
        {
        }

        public void AddField(string name, FieldType type, bool isRequired, string defaultValue = null)
        {
            if (_fields.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Field '{name}' already exists");

            _fields.Add(new PermitField(name, type, isRequired, defaultValue));
            AddDomainEvent(new PermitTypeFieldAddedEvent(Id, name, type));
        }

        public void AddDocumentRequirement(string documentType, bool isRequired, string[] allowedExtensions, long maxFileSize)
        {
            if (_documentRequirements.Any(d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Document requirement '{documentType}' already exists");

            _documentRequirements.Add(new DocumentRequirement(documentType, isRequired, allowedExtensions, maxFileSize));
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
        }

        public void Deactivate(Guid deactivatedByAdminId)
        {
            if (!IsActive)
                return;

            // Note: Business rule "Cannot deactivate if active applications exist" 
            // is checked in command handler (infrastructure concern)
            
            IsActive = false;
            AddDomainEvent(new PermitTypeDeactivatedEvent(Id, deactivatedByAdminId));
        }
    }
}