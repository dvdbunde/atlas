using System;
using Xunit;
using ATLAS.Domain.Entities;

namespace ATLAS.Domain.Tests.Entities
{
    public class UserTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly string _email = "test@example.com";
        private readonly string _firstName = "John";
        private readonly string _lastName = "Doe";

        #region Constructor Tests (Entra Sync Context)

        [Fact]
        public void Create_ShouldInitializeWithCorrectValues()
        {
            // Arrange & Act - User is created during Entra ID sync
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Assert
            Assert.Equal(_userId, user.Id);
            Assert.Equal(_email, user.Email);
            Assert.Equal(_firstName, user.FirstName);
            Assert.Equal(_lastName, user.LastName);
            Assert.Equal(UserRole.Citizen, user.Role);
            Assert.Null(user.LastLoginDate);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenIdIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(Guid.Empty, _email, _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_userId, "", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEmailIsInvalid()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_userId, "invalid-email", _firstName, _lastName, UserRole.Citizen));
            Assert.Contains("valid email", exception.Message.ToLower());
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFirstNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_userId, _email, "", _lastName, UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenLastNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new User(_userId, _email, _firstName, "", UserRole.Citizen));
            Assert.Contains("cannot be empty", exception.Message);
        }

        #endregion

        #region Synchronization Tests (Entra-First)

        [Fact]
        public void SynchronizeFromClaims_WithMatchingValues_ShouldNotModifyProperties()
        {
            // Arrange - User already synchronized with Entra claims
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act - Sync with same values (idempotent)
            user.SynchronizeFromClaims(_email, _firstName, _lastName, UserRole.Citizen);

            // Assert - No changes
            Assert.Equal(_email, user.Email);
            Assert.Equal(_firstName, user.FirstName);
            Assert.Equal(_lastName, user.LastName);
            Assert.Equal(UserRole.Citizen, user.Role);
        }

        [Fact]
        public void SynchronizeFromClaims_WithChangedEmail_ShouldUpdateEmail()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);
            var newEmail = "updated@example.com";

            // Act - Sync from Entra claims with new email
            user.SynchronizeFromClaims(newEmail, _firstName, _lastName, UserRole.Citizen);

            // Assert
            Assert.Equal(newEmail, user.Email);
        }

        [Fact]
        public void SynchronizeFromClaims_WithChangedRole_ShouldUpdateRole()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act - Sync from Entra claims with updated role
            user.SynchronizeFromClaims(_email, _firstName, _lastName, UserRole.Officer);

            // Assert
            Assert.Equal(UserRole.Officer, user.Role);
        }

        [Fact]
        public void SynchronizeFromClaims_WithChangedName_ShouldUpdateName()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act
            user.SynchronizeFromClaims(_email, "Jane", "Smith", UserRole.Citizen);

            // Assert
            Assert.Equal("Jane", user.FirstName);
            Assert.Equal("Smith", user.LastName);
        }

        [Fact]
        public void SynchronizeFromClaims_WithEmptyValues_ShouldNotOverwrite()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act - Sync with empty values (should be ignored)
            user.SynchronizeFromClaims("", "", "", UserRole.Citizen);

            // Assert - Original values preserved
            Assert.Equal(_email, user.Email);
            Assert.Equal(_firstName, user.FirstName);
            Assert.Equal(_lastName, user.LastName);
        }

        [Fact]
        public void SynchronizeFromClaims_ShouldNotRaiseDomainEvent()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);
            user.ClearDomainEvents();

            // Act - Sync is a passive operation, not identity management
            user.SynchronizeFromClaims(_email, _firstName, _lastName, UserRole.Officer);

            // Assert - No domain events are raised for sync operations
            Assert.Empty(user.DomainEvents);
        }

        #endregion

        #region RecordLogin Tests

        [Fact]
        public void RecordLogin_ShouldUpdateLastLoginDate()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);
            Assert.Null(user.LastLoginDate);

            // Act
            user.RecordLogin();

            // Assert
            Assert.NotNull(user.LastLoginDate);
            Assert.True(user.LastLoginDate <= DateTime.UtcNow);
        }

        #endregion

        #region Display Tests

        [Fact]
        public void GetFullName_ShouldReturnFormattedName()
        {
            // Arrange
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act
            var fullName = user.GetFullName();

            // Assert
            Assert.Equal("John Doe", fullName);
        }

        #endregion

        #region Public Sector Specific Tests

        [Fact]
        public void User_ShouldHaveValidEmail_ForOfficialCommunication()
        {
            // Arrange & Act - User created during Entra sync
            var user = new User(_userId, "official@government.gov", _firstName, _lastName, UserRole.Officer);

            // Assert
            Assert.Contains("@government.gov", user.Email);
        }

        [Fact]
        public void User_ShouldStoreRole_FromEntraClaims()
        {
            // Arrange - Roles originate from Entra ID app roles
            var citizen = new User(Guid.NewGuid(), _email, _firstName, _lastName, UserRole.Citizen);
            var officer = new User(Guid.NewGuid(), _email, _firstName, _lastName, UserRole.Officer);
            var admin = new User(Guid.NewGuid(), _email, _firstName, _lastName, UserRole.Admin);

            // Assert - Roles are stored as synchronized from Entra
            Assert.Equal(UserRole.Citizen, citizen.Role);
            Assert.Equal(UserRole.Officer, officer.Role);
            Assert.Equal(UserRole.Admin, admin.Role);
        }

        [Fact]
        public void Role_ShouldOnlyBeUpdated_FromEntraClaims()
        {
            // Arrange - Role set during Entra sync
            var user = new User(_userId, _email, _firstName, _lastName, UserRole.Citizen);

            // Act - Role can only change via SynchronizeFromClaims (Entra-driven)
            user.SynchronizeFromClaims(_email, _firstName, _lastName, UserRole.Admin);

            // Assert
            Assert.Equal(UserRole.Admin, user.Role);
        }

        #endregion
    }
}
