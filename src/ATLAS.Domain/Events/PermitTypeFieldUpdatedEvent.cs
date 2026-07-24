using System;
using ATLAS.Domain.Enums;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFieldUpdatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public Guid FieldId { get; }
        public string FieldName { get; }
        public FieldType FieldType { get; }
        public bool IsRequired { get; }
        public DateTime Timestamp { get; }

        public PermitTypeFieldUpdatedEvent(Guid permitTypeId, Guid fieldId, string fieldName, FieldType fieldType, bool isRequired)
        {
            PermitTypeId = permitTypeId;
            FieldId = fieldId;
            FieldName = fieldName;
            FieldType = fieldType;
            IsRequired = isRequired;
            Timestamp = DateTime.UtcNow;
        }
    }
}
