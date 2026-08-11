//----------------------
// AcsEmailService Tests
// Verifies the ACS email sender: successful send, correct recipient/sender/subject/body,
// and failure propagation. Uses a fake IEmailClient so no live ACS resource is required.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Options;
using ATLAS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class AcsEmailServiceTests
    {
        private static IOptions<EmailOptions> CreateOptions(string senderAddress = "DoNotReply@atlas.com")
        {
            return Microsoft.Extensions.Options.Options.Create(new EmailOptions
            {
                Acs = new AcsEmailOptions
                {
                    Endpoint = "https://atlas-comm-test.communication.azure.com",
                    SenderAddress = senderAddress
                }
            });
        }

        [Fact]
        public async Task SendAsync_DelegatesToEmailClient_WithConfiguredSender_PlainText()
        {
            var client = new FakeEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await service.SendAsync("citizen@example.com", "Subject", "Body", isHtml: false);

            Assert.Equal("DoNotReply@atlas.com", client.SenderAddress);
            Assert.Equal("citizen@example.com", client.RecipientAddress);
            Assert.Equal("Subject", client.Subject);
            Assert.Equal("Body", client.Content);
            Assert.False(client.IsHtml);
        }

        [Fact]
        public async Task SendAsync_HonorsIsHtml_True()
        {
            var client = new FakeEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await service.SendAsync("citizen@example.com", "Subject", "<p>Body</p>", isHtml: true);

            Assert.Equal("<p>Body</p>", client.Content);
            Assert.True(client.IsHtml);
        }

        [Fact]
        public async Task SendAsync_HonorsIsHtml_False()
        {
            var client = new FakeEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await service.SendAsync("citizen@example.com", "Subject", "Plain body", isHtml: false);

            Assert.Equal("Plain body", client.Content);
            Assert.False(client.IsHtml);
        }

        [Fact]
        public async Task SendAsync_Completes_WhenClientSucceeds()
        {
            var client = new FakeEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await service.SendAsync("citizen@example.com", "Subject", "Body");

            Assert.True(client.WasCalled);
        }

        [Fact]
        public async Task SendAsync_PropagatesFailure_WhenClientThrows()
        {
            var client = new ThrowingEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendAsync("citizen@example.com", "Subject", "Body"));
        }

        [Fact]
        public void Constructor_Throws_WhenSenderAddressMissing()
        {
            var client = new FakeEmailClient();
            var options = CreateOptions(senderAddress: "");

            Assert.Throws<InvalidOperationException>(() =>
                new AcsEmailService(client, options, NullLogger<AcsEmailService>.Instance));
        }

        private sealed class FakeEmailClient : IEmailClient
        {
            public string? SenderAddress { get; private set; }
            public string? RecipientAddress { get; private set; }
            public string? Subject { get; private set; }
            public string? Content { get; private set; }
            public bool IsHtml { get; private set; }
            public bool WasCalled { get; private set; }

            public Task SendAsync(string senderAddress, string recipientAddress, string subject, string content, bool isHtml = false, CancellationToken cancellationToken = default)
            {
                SenderAddress = senderAddress;
                RecipientAddress = recipientAddress;
                Subject = subject;
                Content = content;
                IsHtml = isHtml;
                WasCalled = true;
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingEmailClient : IEmailClient
        {
            public Task SendAsync(string senderAddress, string recipientAddress, string subject, string content, bool isHtml = false, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("ACS failure");
        }
    }
}
