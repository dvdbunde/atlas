//----------------------
// BlobEmailTemplateStore Tests
// Exercises the resolution/save/reset logic of the blob-backed store using an
// in-memory blob client (no live Azure). Default templates are served from a
// dedicated temp directory so the deployed Templates/Emails directory is never
// read or mutated by tests.
//----------------------

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Infrastructure.EmailTemplates;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EmailTemplates
{
    public class BlobEmailTemplateStoreTests
    {
        private readonly string _defaultDir;
        private readonly InMemoryEmailTemplateBlobClient _blob;
        private readonly BlobEmailTemplateStore _store;

        private const string Known = "ApprovalNotification";
        private const string BlobName = "email-templates/ApprovalNotification.txt";

        public BlobEmailTemplateStoreTests()
        {
            _defaultDir = Path.Combine(Path.GetTempPath(), "atlas-maildef-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_defaultDir);
            File.WriteAllText(Path.Combine(_defaultDir, "ApprovalNotification.txt"), "DEFAULT {{CitizenName}}");

            _blob = new InMemoryEmailTemplateBlobClient();
            _store = WithBlob(_blob);
        }

        private static string DefaultContent(string dir, string name) =>
            File.ReadAllText(Path.Combine(dir, $"{name}.txt"));

        private BlobEmailTemplateStore WithBlob(IEmailTemplateBlobClient? blob) =>
            new(blob, _defaultDir, NullLogger<BlobEmailTemplateStore>.Instance);

        // -- Template resolution -------------------------------------------------

        [Fact]
        public async Task GetByNameAsync_PrefersCustomizedBlob_WhenItExists()
        {
            await _blob.UploadAsync(BlobName, "CUSTOMIZED {{CitizenName}}");

            var loaded = await _store.GetByNameAsync(Known);

            Assert.NotNull(loaded);
            Assert.Equal("CUSTOMIZED {{CitizenName}}", loaded!.Content);
        }

        [Fact]
        public async Task GetByNameAsync_FallsBackToSourceDefault_WhenNoBlobExists()
        {
            var loaded = await _store.GetByNameAsync(Known);

            Assert.NotNull(loaded);
            Assert.Equal(DefaultContent(_defaultDir, Known), loaded!.Content);
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNull_ForUnknownTemplate()
        {
            await _blob.UploadAsync("email-templates/Unknown.txt", "x");

            var loaded = await _store.GetByNameAsync("Unknown");

            Assert.Null(loaded);
        }

        [Fact]
        public async Task GetByNameAsync_PropagatesStorageFailure_WhenDownloadThrows()
        {
            var failing = new ThrowingEmailTemplateBlobClient();
            var store = WithBlob(failing);
            await failing.UploadAsync(BlobName, "x"); // exists, but download throws

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                store.GetByNameAsync(Known));
        }

        // -- Saving --------------------------------------------------------------

        [Fact]
        public async Task SaveAsync_WritesCustomizedBlob()
        {
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });

            Assert.True(await _blob.ExistsAsync(BlobName));
            Assert.Equal("Custom", await _blob.DownloadAsync(BlobName));
        }

        [Fact]
        public async Task SaveAsync_ReplacesExistingCustomizedBlob_NotVersions()
        {
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "v1" });
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "v2" });

            Assert.Equal(1, _blob.Count);
            Assert.Equal("v2", await _blob.DownloadAsync(BlobName));
        }

        [Fact]
        public async Task SaveAsync_Throws_ForUnknownTemplate()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.SaveAsync(new EmailTemplate { Name = "Evil", Content = "x" }));
        }

        [Fact]
        public async Task SaveAsync_DoesNotOverwriteSourceDefault_WhenBlobConfigured()
        {
            var defaultBefore = DefaultContent(_defaultDir, Known);

            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });

            Assert.Equal(defaultBefore, DefaultContent(_defaultDir, Known));
            Assert.Equal("Custom", await _blob.DownloadAsync(BlobName));
        }

        // -- Reset ---------------------------------------------------------------

        [Fact]
        public async Task ResetAsync_DeletesCustomizedBlob()
        {
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });
            Assert.True(await _blob.ExistsAsync(BlobName));

            await _store.ResetAsync(Known);

            Assert.False(await _blob.ExistsAsync(BlobName));
            Assert.Equal(0, _blob.Count);
        }

        [Fact]
        public async Task ResetAsync_ThenGet_ReturnsSourceDefault()
        {
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });

            await _store.ResetAsync(Known);
            var loaded = await _store.GetByNameAsync(Known);

            Assert.Equal(DefaultContent(_defaultDir, Known), loaded!.Content);
        }

        [Fact]
        public async Task ResetAsync_IsIdempotent_WhenNoBlobExists()
        {
            // No customization exists; reset succeeds without error.
            await _store.ResetAsync(Known);

            Assert.NotNull(await _store.GetByNameAsync(Known));
        }

        [Fact]
        public async Task ResetAsync_Throws_ForUnknownTemplate()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.ResetAsync("Evil"));
        }

        [Fact]
        public async Task ResetAsync_DoesNotWriteDefaultIntoBlob()
        {
            // Reset must delete the customization, not copy the default into Blob.
            await _store.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });

            await _store.ResetAsync(Known);

            Assert.Equal(0, _blob.Count);
        }

        // -- Fallback when Blob not configured ----------------------------------

        [Fact]
        public async Task WithoutBlobClient_FallsBackToFileStoreDefaults()
        {
            var fileOnly = WithBlob(null);

            var loaded = await fileOnly.GetByNameAsync(Known);

            Assert.NotNull(loaded);
            Assert.Equal(DefaultContent(_defaultDir, Known), loaded!.Content);
        }

        [Fact]
        public async Task WithoutBlobClient_ResetIsSafeNoOp()
        {
            var fileOnly = WithBlob(null);

            // Should not throw.
            await fileOnly.ResetAsync(Known);
            Assert.NotNull(await fileOnly.GetByNameAsync(Known));
        }

        [Fact]
        public async Task WithoutBlobClient_SaveIsIgnored_AndDefaultRemains()
        {
            var fileOnly = WithBlob(null);

            await fileOnly.SaveAsync(new EmailTemplate { Name = Known, Content = "Custom" });

            var loaded = await fileOnly.GetByNameAsync(Known);
            Assert.Equal(DefaultContent(_defaultDir, Known), loaded!.Content);
        }

        // -- Test double that throws on download ---------------------------------

        private sealed class ThrowingEmailTemplateBlobClient : IEmailTemplateBlobClient
        {
            private readonly InMemoryEmailTemplateBlobClient _inner = new();

            public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
                => _inner.ExistsAsync(blobName, cancellationToken);

            public Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("blob storage failure");

            public Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
                => _inner.UploadAsync(blobName, content, cancellationToken);

            public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
                => _inner.DeleteAsync(blobName, cancellationToken);
        }

        // -- Full lifecycle (regression for the local-defect) ---------------------------

        /// <summary>
        /// Ordered lifecycle proving that a blob-backed store actually persists customizations
        /// (regression for the reported defect where local Save appeared to succeed but uploaded
        /// nothing). Uses the in-memory blob client so it runs without Azurite. The Azure
        /// connection-string path that feeds a BlobServiceClient into this store is covered by
        /// ServiceCollectionRegistrationTests; combined, they prove local Save persists.
        /// </summary>
        [Fact]
        public async Task Lifecycle_DefaultThenCustomizeThenReset()
        {
            // A known template whose default file exists in _defaultDir.
            const string name = Known;

            // 1. No customized blob -> source-code default is returned.
            var initial = await _store.GetByNameAsync(name);
            Assert.Equal(DefaultContent(_defaultDir, name), initial!.Content);

            // 2. Save a customized template.
            await _store.SaveAsync(new EmailTemplate { Name = name, Content = "CUSTOM" });

            // 3. Confirm the customized blob actually exists.
            Assert.True(await _blob.ExistsAsync(BlobName));

            // 4. Reading now returns the customized content.
            var customized = await _store.GetByNameAsync(name);
            Assert.Equal("CUSTOM", customized!.Content);

            // 5. Reset deletes the customization.
            await _store.ResetAsync(name);

            // 6. Confirm the customized blob is gone.
            Assert.False(await _blob.ExistsAsync(BlobName));

            // 7. Reading again returns the source-code default.
            var restored = await _store.GetByNameAsync(name);
            Assert.Equal(DefaultContent(_defaultDir, name), restored!.Content);
        }
    }
}
