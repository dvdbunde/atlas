//----------------------
// ServiceCollection Registration Tests
// Tests for AddInfrastructure DI registration of Azure services
//----------------------

using System;
using ATLAS.Infrastructure.Options;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Data
{
    public class ServiceCollectionRegistrationTests
    {
        private static IConfiguration CreateConfiguration(Action<Dictionary<string, string?>> configure)
        {
            var values = new Dictionary<string, string?>
            {
                ["Storage:AccountName"] = "",
                ["Storage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["Storage:ContainerName"] = "permit-documents",
                ["Storage:SasTokenExpiryHours"] = "1"
            };
            configure(values);
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [Fact]
        public void AddInfrastructure_WithAccountName_RegistersBlobServiceClient()
        {
            var config = CreateConfiguration(v => v["Storage:AccountName"] = "testaccount");
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var client = provider.GetService<BlobServiceClient>();
            Assert.NotNull(client);
        }

        [Fact]
        public void AddInfrastructure_WithoutAccountName_DoesNotRegisterBlobServiceClient()
        {
            var config = CreateConfiguration(v => { });
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var client = provider.GetService<BlobServiceClient>();
            Assert.Null(client);
        }

        [Fact]
        public void AddInfrastructure_RegistersStorageOptions()
        {
            var config = CreateConfiguration(v => { });
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>();
            Assert.NotNull(options);
            Assert.Equal("UseDevelopmentStorage=true", options.Value.ConnectionString);
            Assert.Equal("permit-documents", options.Value.ContainerName);
        }

        [Fact]
        public void AddInfrastructure_WithNullServices_ThrowsArgumentNullException()
        {
            var config = CreateConfiguration(v => { });
            Assert.Throws<ArgumentNullException>(() =>
                ((IServiceCollection)null!).AddInfrastructure(config));
        }

        [Fact]
        public void AddInfrastructure_WithNullConfiguration_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() =>
                services.AddInfrastructure(null!));
        }
    }
}
