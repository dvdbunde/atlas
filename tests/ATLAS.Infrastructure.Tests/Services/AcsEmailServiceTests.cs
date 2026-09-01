//----------------------
// AcsEmailService Tests
// Verifies the ACS email sender: successful send, correct recipient/sender/subject/body,
// and failure propagation. Uses a fake IEmailClient so no live ACS resource is required.
//----------------------

#nullable enable

using System;
using System.Diagnostics.Metrics;
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

        // ------------------------------------------------------------------
        // O4 metrics: atlas.email.sends / atlas.email.duration
        // ------------------------------------------------------------------

        [Fact]
        public async Task SendAsync_Success_RecordsSendsAndDurationWithOutcomeSuccess()
        {
            using var listener = new MetricsListener();
            var client = new FakeEmailClient();
            var service = new AcsEmailService(client, CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await service.SendAsync("citizen@example.com", "Subject", "Body");

            var sends = listener.GetMeasurement("atlas.email.sends");
            Assert.NotNull(sends);
            Assert.Equal(1, sends!.Value.Value);
            Assert.Single(sends.Value.Tags);
            AssertOutcomeTag(sends.Value.Tags, "success");

            var duration = listener.GetMeasurement("atlas.email.duration");
            Assert.NotNull(duration);
            Assert.True(duration!.Value.Value >= 0);
            Assert.Single(duration.Value.Tags);
            AssertOutcomeTag(duration.Value.Tags, "success");
        }

        [Fact]
        public async Task SendAsync_Failure_RecordsSendsAndDurationWithOutcomeFailure()
        {
            using var listener = new MetricsListener();
            var service = new AcsEmailService(new ThrowingEmailClient(), CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendAsync("citizen@example.com", "Subject", "Body"));

            var sends = listener.GetMeasurement("atlas.email.sends");
            Assert.NotNull(sends);
            Assert.Equal(1, sends!.Value.Value);
            AssertOutcomeTag(sends.Value.Tags, "failure");

            var duration = listener.GetMeasurement("atlas.email.duration");
            Assert.NotNull(duration);
            AssertOutcomeTag(duration!.Value.Tags, "failure");
        }

        [Fact]
        public async Task SendAsync_Cancellation_DoesNotIncrementFailureCounter()
        {
            using var listener = new MetricsListener();
            var service = new AcsEmailService(new CancellingEmailClient(), CreateOptions(), NullLogger<AcsEmailService>.Instance);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.SendAsync("citizen@example.com", "Subject", "Body"));

            // Cancellation is neither a success nor a delivery failure:
            // no sends counter measurement at all.
            Assert.False(listener.GetMeasurement("atlas.email.sends").HasValue);

            // Duration is still recorded (without an outcome tag).
            var duration = listener.GetMeasurement("atlas.email.duration");
            Assert.NotNull(duration);
            Assert.Empty(duration!.Value.Tags);
        }

        private static void AssertOutcomeTag(KeyValuePair<string, object?>[] tags, string expected)
        {
            var tag = Assert.Single(tags);
            Assert.Equal("outcome", tag.Key);
            Assert.Equal(expected, tag.Value);
        }

        /// <summary>
        /// Lightweight MeterListener that captures ATLAS metric measurements
        /// in memory — no Azure dependency.
        /// </summary>
        private sealed class MetricsListener : IDisposable
        {
            private readonly MeterListener _listener = new();
            private readonly Dictionary<string, (double Value, KeyValuePair<string, object?>[] Tags)> _measurements = new();

            public MetricsListener()
            {
                _listener.InstrumentPublished = (instrument, enable) =>
                {
                    if (instrument.Meter.Name == "ATLAS.Application")
                    {
                        enable.EnableMeasurementEvents(instrument);
                    }
                };
                _listener.SetMeasurementEventCallback<long>((inst, measurement, tags, _) =>
                    Capture(inst.Name, measurement, tags));
                _listener.SetMeasurementEventCallback<double>((inst, measurement, tags, _) =>
                    Capture(inst.Name, measurement, tags));
                _listener.Start();
            }

            private void Capture(string name, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            {
                _measurements[name] = (value, tags.ToArray());
            }

            public (double Value, KeyValuePair<string, object?>[] Tags)? GetMeasurement(string instrumentName)
                => _measurements.TryGetValue(instrumentName, out var m) ? m : null;

            public void Dispose() => _listener.Dispose();
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

        private sealed class CancellingEmailClient : IEmailClient
        {
            public Task SendAsync(string senderAddress, string recipientAddress, string subject, string content, bool isHtml = false, CancellationToken cancellationToken = default)
                => throw new OperationCanceledException();
        }

        private sealed class ThrowingEmailClient : IEmailClient
        {
            public Task SendAsync(string senderAddress, string recipientAddress, string subject, string content, bool isHtml = false, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("ACS failure");
        }
    }
}
