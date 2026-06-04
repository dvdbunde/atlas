namespace ATLAS.Domain.Aggregates
{
    // User Aggregate Root - Manages user identity and role
    // Simple entity with no child entities (per ADR-004)
    public class UserAggregate
    {
        private readonly Entities.User _user;

        public Entities.User User => _user;

        public UserAggregate(Entities.User user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
        }

        // Enforce invariants
        public void ValidateInvariants()
        {
            // Email must be valid
            if (string.IsNullOrWhiteSpace(_user.Email) || !_user.Email.Contains("@") || !_user.Email.Contains("."))
                throw new DomainException("Invalid email format");

            // First and last name must be provided
            if (string.IsNullOrWhiteSpace(_user.FirstName))
                throw new DomainException("First name is required");
            
            if (string.IsNullOrWhiteSpace(_user.LastName))
                throw new DomainException("Last name is required");
        }
    }
}