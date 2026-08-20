using System;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Infrastructure.EmailTemplates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EmailTemplates
{
    public class FileEmailTemplateStoreTests
    {
        private readonly FileEmailTemplateStore _store;

        public FileEmailTemplateStoreTests()
        {
            _store = new FileEmailTemplateStore(
                new ConfigurationBuilder().Build(),
                NullLogger<FileEmailTemplateStore>.Instance);
        }

        [Fact]
        public async Task GetTemplateNamesAsync_ReturnsFiveKnownTemplates()
        {
            var names = await _store.GetTemplateNamesAsync();
            Assert.Equal(5, names.Count);
            Assert.Contains("SubmissionConfirmation", names);
            Assert.Contains("ReSubmissionConfirmation", names);
            Assert.Contains("ApprovalNotification", names);
            Assert.Contains("RejectionNotification", names);
            Assert.Contains("InfoRequestNotification", names);
        }

        [Fact]
        public async Task SaveAndGet_RoundTripsContent()
        {
            await _store.SaveAsync(new EmailTemplate { Name = "ApprovalNotification", Content = "Hello {{CitizenName}}" });
            var loaded = await _store.GetByNameAsync("ApprovalNotification");
            Assert.NotNull(loaded);
            Assert.Equal("Hello {{CitizenName}}", loaded!.Content);
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNull_ForUnknownTemplate()
        {
            var loaded = await _store.GetByNameAsync("EvilTemplate");
            Assert.Null(loaded);
        }

        [Fact]
        public async Task SaveAsync_Throws_ForUnknownTemplate()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.SaveAsync(new EmailTemplate { Name = "EvilTemplate", Content = "x" }));
        }

        [Fact]
        public async Task SaveAsync_RejectsPathTraversalName()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.SaveAsync(new EmailTemplate { Name = "../escaped", Content = "x" }));
        }

        [Fact]
        public async Task ResetAsync_IsNoOp_ForKnownTemplate()
        {
            // The file store IS the source-code default; resetting leaves it untouched.
            await _store.ResetAsync("RejectionNotification");
            Assert.NotNull(await _store.GetByNameAsync("RejectionNotification"));
        }

        [Fact]
        public async Task ResetAsync_Throws_ForUnknownTemplate()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.ResetAsync("EvilTemplate"));
        }
    }
}

