using System;

namespace ATLAS.Domain.Entities
{
    public class AuditLog : Entity<Guid>
    {
        public Guid? UserId { get; private set; }
        public string Action { get; private set; }
        public string EntityType { get; private set; }
        public Guid EntityId { get; private set; }
        public string Details { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string IpAddress { get; private set; }

        public AuditLog(Guid? userId, string action, string entityType, Guid entityId, string details, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be empty", nameof(action));
            
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be empty", nameof(entityType));
            
            if (entityId == Guid.Empty)
                throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));

            if (!userId.HasValue || userId.Value == Guid.Empty)
                throw new ArgumentException("User ID must be a valid, authenticated user", nameof(userId));

            Id = Guid.NewGuid();
            UserId = userId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            Details = details ?? string.Empty;
            Timestamp = DateTime.UtcNow;
            IpAddress = ipAddress ?? string.Empty;
        }

        protected AuditLog()
        {
        }

        // AuditLog entries are immutable - no update or delete methods
        // This enforces the 7-year retention policy from PRD F-20
    }
}