using System;
using ATLAS.Domain.Email;
using Xunit;

namespace ATLAS.Application.Tests.EmailTemplates
{
    public class EmailTemplateUpdatedEventTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var userId = Guid.NewGuid();
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification", userId);

            Assert.Equal("ApprovalNotification", evt.TemplateName);
            Assert.Equal(userId, evt.PerformedByUserId);
        }

        [Fact]
        public void EntityId_IsDeterministicForSameName()
        {
            var a = new EmailTemplateUpdatedEvent("ApprovalNotification", Guid.NewGuid());
            var b = new EmailTemplateUpdatedEvent("ApprovalNotification", Guid.NewGuid());

            Assert.Equal(a.EntityId, b.EntityId);
        }

        [Fact]
        public void EntityId_DiffersAcrossNames()
        {
            var a = new EmailTemplateUpdatedEvent("ApprovalNotification", Guid.NewGuid());
            var b = new EmailTemplateUpdatedEvent("RejectionNotification", Guid.NewGuid());

            Assert.NotEqual(a.EntityId, b.EntityId);
        }

        [Fact]
        public void EntityId_IsNotGuidEmpty()
        {
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification", Guid.NewGuid());
            Assert.NotEqual(Guid.Empty, evt.EntityId);
        }
    }
}

