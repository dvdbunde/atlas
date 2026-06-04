using System;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFieldAddedEvent
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
