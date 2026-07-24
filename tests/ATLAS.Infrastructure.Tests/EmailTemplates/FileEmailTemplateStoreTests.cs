using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private readonly string _root;
        private readonly FileEmailTemplateStore _store;

        public FileEmailTemplateStoreTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "atlas-emailtests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Templates:Path"] = _root
                })
                .Build();
            _store = new FileEmailTemplateStore(config, NullLogger<FileEmailTemplateStore>.Instance);
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
        public async Task ResolveFile_ConfinesToRootDirectory()
        {
            // Even a name that resolves inside root is allowed; traversal is blocked.
            await _store.SaveAsync(new EmailTemplate { Name = "RejectionNotification", Content = "x" });
            var file = Path.Combine(_root, "RejectionNotification.txt");
            Assert.True(File.Exists(file));
        }
    }
}

