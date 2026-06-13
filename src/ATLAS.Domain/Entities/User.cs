using System;

namespace ATLAS.Domain.Entities
{
    /// <summary>
    /// User aggregate representing a synchronized local representation of an Entra ID principal.
    /// This entity is NOT an identity account - it is a business entity that references an external identity.
    /// 
    /// Responsibilities:
    /// - External identity reference (Entra ID object ID)
    /// - Business relationships (case ownership, application ownership)
    /// - Audit ownership (tracking who performed actions)
    /// - Reporting ownership (assigning work to users)
    /// 
    /// Non-responsibilities:
    /// - User authentication (delegated to Entra ID)
    /// - User authorization (roles managed in Entra ID)
    /// - User profile management (synchronized from Entra claims)
    /// - User lifecycle management (created/deactivated in Entra ID)
    /// </summary>
    public class User : Entity<Guid>
    {
        /// <summary>
        /// User's email address - synchronized from Entra ID claims
        /// </summary>
        public string Email { get; private set; }
        
        /// <summary>
        /// User's first name - synchronized from Entra ID claims
        /// </summary>
        public string FirstName { get; private set; }
        
        /// <summary>
        /// User's last name - synchronized from Entra ID claims
        /// </summary>
        public string LastName { get; private set; }
        
        /// <summary>
        /// User's role - synchronized from Entra ID app roles
        /// </summary>
        public UserRole Role { get; private set; }
        
        /// <summary>
        /// Timestamp of last login - updated on each authentication/synchronization
        /// </summary>
        public DateTime? LastLoginDate { get; private set; }

        /// <summary>
        /// Creates a User synchronized from Entra ID claims.
        /// This constructor is used by IdentityResolver when a new user is encountered.
        /// </summary>
        /// <param name="id">Entra ID object ID (oid claim) - must match the GUID from Entra</param>
        /// <param name="email">Email address from Entra claims</param>
        /// <param name="firstName">First name from Entra claims (given_name)</param>
        /// <param name="lastName">Last name from Entra claims (family_name)</param>
        /// <param name="role">Role from Entra app roles</param>
        public User(Guid id, string email, string firstName, string lastName, UserRole role)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID cannot be empty", nameof(id));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));
            
            if (!email.Contains("@") || !email.Contains("."))
                throw new ArgumentException("Invalid email format", nameof(email));
            
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));
            
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            Id = id;
            Email = email.ToLowerInvariant();
            FirstName = firstName;
            LastName = lastName;
            Role = role;
            LastLoginDate = null;
        }

        /// <summary>
        /// Parameterless constructor for EF Core
        /// </summary>
        protected User()
        {
        }

        /// <summary>
        /// Records a login event - called by IdentityResolver during synchronization
        /// </summary>
        public void RecordLogin()
        {
            LastLoginDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the user'\''s full name for display purposes
        /// </summary>
        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }

    /// <summary>
    /// User roles - these values must match Entra ID app roles
    /// </summary>
    public enum UserRole
    {
        Citizen = 1,
        Officer = 2,
        Admin = 3
    }
}
