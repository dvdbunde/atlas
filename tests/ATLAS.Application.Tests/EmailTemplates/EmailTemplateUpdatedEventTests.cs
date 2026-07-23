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
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification");

            Assert.Equal("ApprovalNotification", evt.TemplateName);
        }

        [Fact]
        public void EntityId_IsDeterministicForSameName()
        {
            var a = new EmailTemplateUpdatedEvent("ApprovalNotification");
            var b = new EmailTemplateUpdatedEvent("ApprovalNotification");

            Assert.Equal(a.EntityId, b.EntityId);
        }

        [Fact]
        public void EntityId_DiffersAcrossNames()
        {
            var a = new EmailTemplateUpdatedEvent("ApprovalNotification");
            var b = new EmailTemplateUpdatedEvent("RejectionNotification");

            Assert.NotEqual(a.EntityId, b.EntityId);
        }

        [Fact]
        public void EntityId_IsNotGuidEmpty()
        {
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification");
            Assert.NotEqual(Guid.Empty, evt.EntityId);
        }
    }
}

