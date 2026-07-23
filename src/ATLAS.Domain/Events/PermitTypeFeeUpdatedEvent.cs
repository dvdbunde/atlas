using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFeeUpdatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public decimal OldFee { get; }
        public decimal NewFee { get; }
        public DateTime Timestamp { get; }

        public PermitTypeFeeUpdatedEvent(Guid permitTypeId, decimal oldFee, decimal newFee)
        {
            PermitTypeId = permitTypeId;
            OldFee = oldFee;
            NewFee = newFee;
            Timestamp = DateTime.UtcNow;
        }
    }
}