using System;
using ATLAS.Domain.Aggregates;
using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Aggregates
{
    public class UserAggregateTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithUser()
        {
            // Arrange & Act
            var user = new User("test@example.com", "John", "Doe", UserRole.Citizen);
            var aggregate = new UserAggregate(user);

            // Assert
            Assert.Equal(user, aggregate.User);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenUserIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserAggregate(null!));
            Assert.Contains("user", exception.Message);
        }

        [Fact]
        public void ValidateInvariants_ShouldPass_WhenValidUser()
        {
            // Arrange
            var user = new User("test@example.com", "John", "Doe", UserRole.Citizen);
            var aggregate = new UserAggregate(user);

            // Act & Assert (should not throw)
            aggregate.ValidateInvariants();
        }

        [Fact]
        public void ValidateInvariants_ShouldThrowException_WhenInvalidEmail()
        {
            // Arrange
            var user = new User("test@example.com", "John", "Doe", UserRole.Citizen);
            // Use reflection to set invalid email
            var property = typeof(User).GetProperty("Email");
            property!.SetValue(user, "invalid-email");

            var aggregate = new UserAggregate(user);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.ValidateInvariants());
            Assert.Contains("Invalid email", exception.Message);
        }

        [Fact]
        public void ValidateInvariants_ShouldThrowException_WhenMissingFirstName()
        {
            // Arrange
            var user = new User("test@example.com", "John", "Doe", UserRole.Citizen);
            // Use reflection to set empty first name
            var property = typeof(User).GetProperty("FirstName");
            property!.SetValue(user, "");

            var aggregate = new UserAggregate(user);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.ValidateInvariants());
            Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
