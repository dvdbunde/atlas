using System;
using Xunit;
using ATLAS.Domain.Entities;

namespace ATLAS.Domain.Tests.Entities
{
    public class AuditLogTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly string _action = "APPLICATION_SUBMITTED";
        private readonly string _entityType = "Application";
        private readonly Guid _entityId = Guid.NewGuid();
        private readonly string _details = "Application submitted by citizen";
        private readonly string _ipAddress = "192.168.1.1";

        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var auditLog = new AuditLog(_userId, _action, _entityType, _entityId, _details, _ipAddress);

            // Assert
            Assert.Equal(_userId, auditLog.UserId);
            Assert.Equal(_action, auditLog.Action);
            Assert.Equal(_entityType, auditLog.EntityType);
            Assert.Equal(_entityId, auditLog.EntityId);
            Assert.Equal(_details, auditLog.Details);
            Assert.Equal(_ipAddress, auditLog.IpAddress);
            Assert.True(auditLog.Timestamp <= DateTime.UtcNow);
            Assert.NotEqual(Guid.Empty, auditLog.Id);
        }

        [Fact]
        public void Create_ShouldAllowNullUserId()
        {
            // Arrange & Act
            var auditLog = new AuditLog(null, _action, _entityType, _entityId, _details, _ipAddress);

            // Assert
            Assert.Null(auditLog.UserId);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenActionIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new AuditLog(_userId, "", _entityType, _entityId, _details, _ipAddress));
            Assert.Contains("Action cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEntityTypeIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new AuditLog(_userId, _action, "", _entityId, _details, _ipAddress));
            Assert.Contains("Entity type cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenEntityIdIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new AuditLog(_userId, _action, _entityType, Guid.Empty, _details, _ipAddress));
            Assert.Contains("Entity ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldHandleNullDetails()
        {
            // Arrange & Act
            var auditLog = new AuditLog(_userId, _action, _entityType, _entityId, null, _ipAddress);

            // Assert
            Assert.Equal(string.Empty, auditLog.Details);
        }

        [Fact]
        public void Create_ShouldHandleNullIpAddress()
        {
            // Arrange & Act
            var auditLog = new AuditLog(_userId, _action, _entityType, _entityId, _details, null);

            // Assert
            Assert.Equal(string.Empty, auditLog.IpAddress);
        }

        [Fact]
        public void AuditLog_ShouldBeImmutable()
        {
            // Arrange
            var auditLog = new AuditLog(_userId, _action, _entityType, _entityId, _details, _ipAddress);

            // Assert - Verify properties don't have public setters
            var properties = typeof(AuditLog).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name != "DomainEvents") // DomainEvents is from base class
                {
                    Assert.True(prop.GetSetMethod() == null || prop.GetSetMethod()?.IsPrivate == true,
                        $"Property {prop.Name} should not have a public setter");
                }
            }
        }
    }
}
