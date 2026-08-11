//----------------------
// ServiceCollection Registration Tests
// Tests for AddInfrastructure DI registration of Azure services
//----------------------

using System;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Options;
using ATLAS.Infrastructure.Services;
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
        public void AddInfrastructure_WithConnectionStringOnly_RegistersBlobServiceClient()
        {
            // Local development: a connection string must produce a usable Blob client so
            // customized email templates can actually be persisted to Azurite.
            var config = CreateConfiguration(v => v["Storage:AccountName"] = "");
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var client = provider.GetService<BlobServiceClient>();
            Assert.NotNull(client);
        }

        [Fact]
        public void AddInfrastructure_WithNeither_DoesNotRegisterBlobServiceClient()
        {
            var config = CreateConfiguration(v =>
            {
                v["Storage:AccountName"] = "";
                v["Storage:ConnectionString"] = "";
            });
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var client = provider.GetService<BlobServiceClient>();
            Assert.Null(client);
        }

        [Fact]
        public void AddInfrastructure_WithAccountNamePrefersAzure_NotConnectionString()
        {
            // Even when a connection string is also present, a configured account name must
            // win so production remains Managed Identity / DefaultAzureCredential based.
            var config = CreateConfiguration(v => v["Storage:AccountName"] = "testaccount");
            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var client = provider.GetService<BlobServiceClient>();
            Assert.NotNull(client);
            Assert.EndsWith("testaccount.blob.core.windows.net", client.Uri.Host);
        }

        [Fact]
        public void AddInfrastructure_WithConnectionStringOnly_ConfiguresEmailTemplateStoreAgainstLocalStorage()
        {
            // Regression test for the reported defect: with a connection string configured and
            // no account name (local development), the email-template store must be backed by a
            // Blob Service client so saving actually persists to Azurite instead of being a no-op.
            var config = CreateConfiguration(v => v["Storage:AccountName"] = "");
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var store = provider.GetRequiredService<ATLAS.Application.EmailTemplates.IEmailTemplateStore>();
            Assert.IsType<ATLAS.Infrastructure.EmailTemplates.BlobEmailTemplateStore>(store);

            // The connection string must be usable against the Azurite emulator.
            var client = provider.GetRequiredService<BlobServiceClient>();
            Assert.Equal("127.0.0.1", client.Uri.Host);
            Assert.Equal(10000, client.Uri.Port);
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

        [Fact]
        public void AddInfrastructure_WithAcsConfigured_ResolvesAcsEmailService()
        {
            var config = CreateConfiguration(v =>
            {
                v["Email:Acs:Endpoint"] = "https://atlas-comm-test.communication.azure.com";
                v["Email:Acs:SenderAddress"] = "DoNotReply@atlas.com";
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var emailService = provider.GetRequiredService<IEmailService>();
            Assert.IsType<AcsEmailService>(emailService);
        }

        [Fact]
        public void AddInfrastructure_Development_WithoutAcs_ResolvesLocalEmailService()
        {
            var config = CreateConfiguration(v =>
            {
                v["ASPNETCORE_ENVIRONMENT"] = "Development";
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var emailService = provider.GetRequiredService<IEmailService>();
            Assert.IsType<LocalEmailService>(emailService);
        }

        [Fact]
        public void AddInfrastructure_NonDevelopment_WithoutAcs_Throws()
        {
            // A deployed (non-development) environment must fail fast when ACS email is not
            // configured, rather than silently falling back to the local sink.
            var config = CreateConfiguration(v =>
            {
                v["ASPNETCORE_ENVIRONMENT"] = "Production";
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() =>
                provider.GetRequiredService<IEmailService>());
        }

        [Fact]
        public void AddInfrastructure_NonDevelopment_WithAcs_ResolvesAcsEmailService()
        {
            var config = CreateConfiguration(v =>
            {
                v["ASPNETCORE_ENVIRONMENT"] = "Production";
                v["Email:Acs:Endpoint"] = "https://atlas-comm-test.communication.azure.com";
                v["Email:Acs:SenderAddress"] = "DoNotReply@atlas.com";
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var emailService = provider.GetRequiredService<IEmailService>();
            Assert.IsType<AcsEmailService>(emailService);
        }

        [Fact]
        public void AddInfrastructure_WithAcsConfigured_DoesNotResolveSmtp()
        {
            // There must be no SMTP fallback in production. With ACS configured, the
            // resolved service must be the ACS implementation (SmtpEmailService no longer exists).
            var config = CreateConfiguration(v =>
            {
                v["Email:Acs:Endpoint"] = "https://atlas-comm-test.communication.azure.com";
                v["Email:Acs:SenderAddress"] = "DoNotReply@atlas.com";
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var emailService = provider.GetRequiredService<IEmailService>();
            Assert.IsType<AcsEmailService>(emailService);
        }
    }
}
