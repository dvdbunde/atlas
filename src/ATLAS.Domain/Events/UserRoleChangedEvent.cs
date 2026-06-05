using System;
using ATLAS.Domain.Entities;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class UserRoleChangedEvent : INotification
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
