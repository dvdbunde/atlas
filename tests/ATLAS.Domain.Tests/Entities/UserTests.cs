using System;
using Xunit;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Tests.Entities
{
    public class UserTests
    {
        private readonly string _email = "test@example.com";
        private readonly string _firstName = "John";
        private readonly string _lastName = "Doe";

        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);

            // Assert
            Assert.Equal(_email.ToLowerInvariant(), user.Email);
            Assert.Equal(_firstName, user.FirstName);
            Assert.Equal(_lastName, user.LastName);
            Assert.Equal(UserRole.Citizen, user.Role);
            Assert.True(user.IsActive);
            Assert.Null(user.LastLoginDate);
            Assert.NotEqual(Guid.Empty, user.Id);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User("", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("Email cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailInvalid()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User("invalid-email", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("Invalid email format", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFirstNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_email, "", _lastName, UserRole.Citizen));
            Assert.Contains("First name cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenLastNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_email, _firstName, "", UserRole.Citizen));
            Assert.Contains("Last name cannot be empty", exception.Message);
        }

        [Fact]
        public void ChangeRole_ShouldUpdateRoleAndRaiseEvent()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            var originalRole = user.Role;

            // Act
            user.ChangeRole(UserRole.Officer);

            // Assert
            Assert.Equal(UserRole.Officer, user.Role);
            Assert.Single(user.DomainEvents);
            var domainEvent = Assert.IsType<UserRoleChangedEvent>(user.DomainEvents[0]);
            Assert.Equal(originalRole, domainEvent.OldRole);
            Assert.Equal(UserRole.Officer, domainEvent.NewRole);
        }

        [Fact]
        public void ChangeRole_ShouldNotRaiseEvent_WhenRoleUnchanged()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            user.ClearDomainEvents(); // Clear initial events

            // Act
            user.ChangeRole(UserRole.Citizen); // Same role

            // Assert
            Assert.Equal(UserRole.Citizen, user.Role);
            Assert.Empty(user.DomainEvents);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            Assert.True(user.IsActive);

            // Act
            user.Deactivate();

            // Assert
            Assert.False(user.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldNotChange_WhenAlreadyInactive()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            user.Deactivate(); // First deactivation
            Assert.False(user.IsActive);

            // Act
            user.Deactivate(); // Second deactivation should be idempotent

            // Assert
            Assert.False(user.IsActive); // Should still be false
        }

        [Fact]
        public void RecordLogin_ShouldUpdateLastLoginDate()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            Assert.Null(user.LastLoginDate);

            // Act
            user.RecordLogin();

            // Assert
            Assert.NotNull(user.LastLoginDate);
            Assert.True(user.LastLoginDate <= DateTime.UtcNow);
        }

        [Fact]
        public void GetFullName_ShouldReturnFormattedName()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);

            // Act
            var fullName = user.GetFullName();

            // Assert
            Assert.Equal("John Doe", fullName);
        }
    }
}
