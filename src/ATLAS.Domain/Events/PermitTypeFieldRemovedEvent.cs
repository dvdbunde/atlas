using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFieldRemovedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public Guid FieldId { get; }
        public string FieldName { get; }
        public DateTime Timestamp { get; }

        public PermitTypeFieldRemovedEvent(Guid permitTypeId, Guid fieldId, string fieldName)
        {
            PermitTypeId = permitTypeId;
            FieldId = fieldId;
            FieldName = fieldName;
            Timestamp = DateTime.UtcNow;
        }
    }
}
