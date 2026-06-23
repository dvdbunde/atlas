using System;
using ATLAS.Domain.Enums;
using ATLAS.Domain.ValueObjects;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFieldAddedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public string FieldName { get; }
        public FieldType FieldType { get; }
        public DateTime Timestamp { get; }

        public PermitTypeFieldAddedEvent(Guid permitTypeId, string fieldName, FieldType fieldType)
        {
            PermitTypeId = permitTypeId;
            FieldName = fieldName;
            FieldType = fieldType;
            Timestamp = DateTime.UtcNow;
        }
    }
}
