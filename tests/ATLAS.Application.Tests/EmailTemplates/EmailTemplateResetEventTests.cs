using System;
using ATLAS.Domain.Email;
using Xunit;

namespace ATLAS.Application.Tests.EmailTemplates
{
    public class EmailTemplateResetEventTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var evt = new EmailTemplateResetEvent("ApprovalNotification");

            Assert.Equal("ApprovalNotification", evt.TemplateName);
        }

        [Fact]
        public void EntityId_IsDeterministicForSameName()
        {
            var a = new EmailTemplateResetEvent("ApprovalNotification");
            var b = new EmailTemplateResetEvent("ApprovalNotification");

            Assert.Equal(a.EntityId, b.EntityId);
        }

        [Fact]
        public void EntityId_MatchesUpdateEvent_ForSameTemplate()
        {
            // Reset and update target the same template entity, so they must share an
            // audit id to keep a single audit trail per template.
            var reset = new EmailTemplateResetEvent("ApprovalNotification");
            var update = new EmailTemplateUpdatedEvent("ApprovalNotification");

            Assert.Equal(update.EntityId, reset.EntityId);
        }

        [Fact]
        public void EntityId_IsNotGuidEmpty()
        {
            var evt = new EmailTemplateResetEvent("ApprovalNotification");
            Assert.NotEqual(Guid.Empty, evt.EntityId);
        }
    }
}
