using System;
using System.Collections.Generic;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Events;
using ATLAS.Domain.Enums;

namespace ATLAS.Domain.Entities
{
    public class PermitType : Entity<Guid>
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public decimal Fee { get; private set; }
        
        private readonly List<PermitField> _fields = new();
        public IReadOnlyList<PermitField> Fields => _fields.OrderBy(f => f.Order).ToList().AsReadOnly();
        
        private readonly List<DocumentRequirement> _documentRequirements = new();
        public IReadOnlyList<DocumentRequirement> DocumentRequirements => _documentRequirements.OrderBy(d => d.Order).ToList().AsReadOnly();

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

        public void UpdateGeneralInformation(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            if (name.Length < 3 || name.Length > 100)
                throw new ArgumentException("Name must be between 3 and 100 characters", nameof(name));

            Name = name;
            Description = description ?? string.Empty;
            AddDomainEvent(new PermitTypeGeneralInformationUpdatedEvent(Id, Name, Description));
        }

        public void AddField(string name, FieldType type, bool isRequired, string defaultValue = null, IReadOnlyCollection<string> options = null)
        {
            if (_fields.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Field '{name}' already exists");
        
            var field = new PermitField(name, type, isRequired, defaultValue, options);
            field.Order = _fields.Count + 1;
            _fields.Add(field);
            AddDomainEvent(new PermitTypeFieldAddedEvent(Id, name, type));
        }

        public void UpdateField(Guid fieldId, string name, FieldType type, bool isRequired, string defaultValue = null, IReadOnlyCollection<string> options = null)
        {
            var field = FindField(fieldId);

            if (!field.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && _fields.Any(f => f.Id != fieldId && f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Field '{name}' already exists");

            field.Update(name, type, isRequired, defaultValue, options);
            AddDomainEvent(new PermitTypeFieldUpdatedEvent(Id, fieldId, name, type, isRequired));
        }

        public void RemoveField(Guid fieldId)
        {
            var field = FindField(fieldId);
            _fields.Remove(field);
            RenumberFields();
            AddDomainEvent(new PermitTypeFieldRemovedEvent(Id, fieldId, field.Name));
        }

        public void MoveField(Guid fieldId, int newOrder)
        {
            var field = FindField(fieldId);
            var ordered = _fields.OrderBy(f => f.Order).ToList();
            ordered.Remove(field);

            if (newOrder < 1 || newOrder > ordered.Count + 1)
                throw new DomainException($"Field order must be between 1 and {ordered.Count + 1}");

            ordered.Insert(newOrder - 1, field);
            ApplyOrder(ordered);
            AddDomainEvent(new PermitTypeFieldsReorderedEvent(Id));
        }

        private PermitField FindField(Guid fieldId)
        {
            var field = _fields.FirstOrDefault(f => f.Id == fieldId);
            if (field == null)
                throw new DomainException($"Field '{fieldId}' was not found");
            return field;
        }

        private void RenumberFields() => ApplyOrder(_fields.OrderBy(f => f.Order).ToList());

        private static void ApplyOrder(IReadOnlyList<PermitField> ordered)
        {
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].Order = i + 1;
        }

        public void AddDocumentRequirement(string documentType, bool isRequired, string[] allowedExtensions, long maxFileSize)
        {
            if (_documentRequirements.Any(d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Document requirement '{documentType}' already exists");

            var requirement = new DocumentRequirement(documentType, isRequired, allowedExtensions, maxFileSize);
            requirement.Order = _documentRequirements.Count + 1;
            _documentRequirements.Add(requirement);
        }

        public void UpdateDocumentRequirement(Guid requirementId, bool isRequired, string[] allowedExtensions, long maxFileSize)
        {
            var requirement = FindDocumentRequirement(requirementId);

            if (allowedExtensions == null || allowedExtensions.Length == 0)
                throw new DomainException("Allowed extensions must be provided");
            if (maxFileSize <= 0)
                throw new DomainException("Max file size must be positive");

            requirement.Update(isRequired, allowedExtensions, maxFileSize);
            AddDomainEvent(new PermitTypeDocumentRequirementUpdatedEvent(Id, requirementId, requirement.DocumentType, isRequired));
        }

        public void RemoveDocumentRequirement(Guid requirementId)
        {
            var requirement = FindDocumentRequirement(requirementId);
            _documentRequirements.Remove(requirement);
            RenumberDocumentRequirements();
            AddDomainEvent(new PermitTypeDocumentRequirementRemovedEvent(Id, requirementId, requirement.DocumentType));
        }

        public void MoveDocumentRequirement(Guid requirementId, int newOrder)
        {
            var requirement = FindDocumentRequirement(requirementId);
            var ordered = _documentRequirements.OrderBy(d => d.Order).ToList();
            ordered.Remove(requirement);

            if (newOrder < 1 || newOrder > ordered.Count + 1)
                throw new DomainException($"Document requirement order must be between 1 and {ordered.Count + 1}");

            ordered.Insert(newOrder - 1, requirement);
            ApplyDocumentRequirementOrder(ordered);
            AddDomainEvent(new PermitTypeDocumentRequirementsReorderedEvent(Id));
        }

        private DocumentRequirement FindDocumentRequirement(Guid requirementId)
        {
            var requirement = _documentRequirements.FirstOrDefault(d => d.Id == requirementId);
            if (requirement == null)
                throw new DomainException($"Document requirement '{requirementId}' was not found");
            return requirement;
        }

        private void RenumberDocumentRequirements() => ApplyDocumentRequirementOrder(_documentRequirements.OrderBy(d => d.Order).ToList());

        private static void ApplyDocumentRequirementOrder(IReadOnlyList<DocumentRequirement> ordered)
        {
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].Order = i + 1;
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

        public void UpdateFee(decimal fee)
        {
            if (fee < 0)
                throw new ArgumentException("Fee cannot be negative", nameof(fee));

            if (Fee == fee)
                return;

            var oldFee = Fee;
            Fee = fee;
            AddDomainEvent(new PermitTypeFeeUpdatedEvent(Id, oldFee, fee));
        }
    }
}