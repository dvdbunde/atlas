using System;
using ATLAS.Domain.Entities;

namespace ATLAS.Domain.Events
{
    public class UserRoleChangedEvent
    {
        public Guid UserId { get; }
        public UserRole OldRole { get; }
        public UserRole NewRole { get; }
        public DateTime Timestamp { get; }

        public UserRoleChangedEvent(Guid userId, UserRole oldRole, UserRole newRole)
        {
            UserId = userId;
            OldRole = oldRole;
            NewRole = newRole;
            Timestamp = DateTime.UtcNow;
        }
    } 
}
