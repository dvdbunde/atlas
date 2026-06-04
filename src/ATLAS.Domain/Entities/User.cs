using System;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Entities
{
    public class User : Entity<Guid>
    {
        public string Email { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? LastLoginDate { get; private set; }

        public User(string email, string firstName, string lastName, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));
            
            if (!email.Contains("@") || !email.Contains("."))
                throw new ArgumentException("Invalid email format", nameof(email));
            
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));
            
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            Id = Guid.NewGuid();
            Email = email.ToLowerInvariant();
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            IsActive = true;
            LastLoginDate = null;
        }

        protected User()
        {
        }

        public void ChangeRole(UserRole newRole)
        {
            if (Role == newRole)
                return;

            var oldRole = Role;
            Role = newRole;
            AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole));
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
        }

        public void RecordLogin()
        {
            LastLoginDate = DateTime.UtcNow;
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }

    public enum UserRole
    {
        Citizen = 1,
        Officer = 2,
        Admin = 3
    }
}