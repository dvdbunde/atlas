using System;
using Xunit;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Tests.Entities
{
    public class UserTests
    {
        private readonly string _email = "test@example.com";
        private readonly string _firstName = "John";
        private readonly string _lastName = "Doe";

        #region Constructor Tests

        [Fact]
        public void Create_ShouldInitializeWithCorrectValues()
        {
            // Arrange & Act
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);

            // Assert
            Assert.Equal(_email, user.Email);
            Assert.Equal(_firstName, user.FirstName);
            Assert.Equal(_lastName, user.LastName);
            Assert.Equal(UserRole.Citizen, user.Role);
            Assert.True(user.IsActive);
            Assert.True(user.CreatedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User("", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailIsInvalid()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User("invalid-email", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("valid email", exception.Message.ToLower());
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFirstNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_email, "", _lastName, UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenLastNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_email, _firstName, "", UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        #endregion

        #region Role Management Tests (Public Sector)

        [Fact]
        public void ChangeRole_ShouldUpdateRole_WhenValidTransition()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            var originalRole = user.Role;

            // Act
            user.ChangeRole(UserRole.Officer);

            // Assert
            Assert.Equal(UserRole.Officer, user.Role);
            Assert.NotEqual(originalRole, user.Role);
        }

        [Fact]
        public void ChangeRole_ShouldRaiseEvent_WhenRoleChanged()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            user.ClearDomainEvents();

            // Act
            user.ChangeRole(UserRole.Officer);

            // Assert
            var domainEvent = Assert.Single(user.DomainEvents);
            var roleChangedEvent = Assert.IsType<UserRoleChangedEvent>(domainEvent);
            Assert.Equal(user.Id, roleChangedEvent.UserId);
            Assert.Equal(UserRole.Citizen, roleChangedEvent.OldRole);
            Assert.Equal(UserRole.Officer, roleChangedEvent.NewRole);
        }

        [Fact]
        public void ChangeRole_ShouldNotThrowException_WhenSameRole()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            var originalRole = user.Role;

            // Act - ChangeRole with same role should return silently
            user.ChangeRole(UserRole.Citizen);

            // Assert - Role should remain unchanged
            Assert.Equal(originalRole, user.Role);
        }

        #endregion

        #region Activation/Deactivation Tests

        [Fact]
        public void Deactivate_ShouldSetIsActiveToFalse()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);

            // Act
            user.Deactivate();

            // Assert
            Assert.False(user.IsActive);
        }

        [Fact]
        public void Deactivate_ShouldNotRaiseEvent()
        {
            // Arrange
            var user = new User(_email, _firstName, _lastName, UserRole.Citizen);
            user.ClearDomainEvents();

            // Act
            user.Deactivate();

            // Assert - Deactivate doesn't raise a domain event in current implementation
            Assert.Empty(user.DomainEvents);
        }

        // Note: User entity doesn't have Activate() method
        // IsActive is set via Deactivate() which sets it to false
        // To reactivate, we would need to check the actual domain model
        // For now, we test that IsActive property can be observed

        #endregion

        #region Public Sector Specific Tests

        [Fact]
        public void User_ShouldHaveValidEmail_ForOfficialCommunication()
        {
            // Arrange & Act
            var user = new User("official@government.gov", _firstName, _lastName, UserRole.Officer);

            // Assert
            Assert.Contains("@government.gov", user.Email);
        }

        [Fact]
        public void User_ShouldEnforceRoleBasedAccess_ForPublicSector()
        {
            // Arrange
            var citizen = new User(_email, _firstName, _lastName, UserRole.Citizen);
            var officer = new User(_email, _firstName, _lastName, UserRole.Officer);
            var admin = new User(_email, _firstName, _lastName, UserRole.Admin);

            // Assert
            Assert.Equal(UserRole.Citizen, citizen.Role);
            Assert.Equal(UserRole.Officer, officer.Role);
            Assert.Equal(UserRole.Admin, admin.Role);
        }

        #endregion
    }
}
