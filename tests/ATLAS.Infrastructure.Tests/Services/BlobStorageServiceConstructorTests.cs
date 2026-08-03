//----------------------
// BlobStorageService Constructor Tests
// Tests for the two-constructor pattern supporting connection-string and Managed Identity
//----------------------

#nullable enable

using System;
using ATLAS.Infrastructure.Options;
using ATLAS.Infrastructure.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class BlobStorageServiceConstructorTests
    {
        private static readonly IOptions<StorageOptions> ValidConnectionOptions = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "permit-documents",
            SasTokenExpiryHours = 1
        });

        private static readonly IOptions<StorageOptions> ValidManagedIdentityOptions = Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            ConnectionString = null,
            ContainerName = "permit-documents",
            SasTokenExpiryHours = 1
        });

        [Fact]
        public void Constructor_WithConnectionString_ShouldSucceed()
        {
            var service = new BlobStorageService(ValidConnectionOptions);
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithBlobServiceClient_ShouldSucceed()
        {
            var service = new BlobStorageService(ValidManagedIdentityOptions, new BlobServiceClient("UseDevelopmentStorage=true"));
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullBlobServiceClient_FallsBackToConnectionString()
        {
            var service = new BlobStorageService(ValidConnectionOptions, null);
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithoutBoth_ThrowsInvalidOperationException()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new StorageOptions
            {
                ConnectionString = null,
                ContainerName = "permit-documents",
                SasTokenExpiryHours = 1
            });

            Assert.Throws<InvalidOperationException>(() => new BlobStorageService(options, null));
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BlobStorageService(null!));
        }

        [Fact]
        public void Constructor_WithNullOptionsAndBlobClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BlobStorageService(null!, new BlobServiceClient("UseDevelopmentStorage=true")));
        }
    }
}
