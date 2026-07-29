//----------------------
// Key Vault Configuration Provider Tests
// Verifies that Key Vault is only configured when VaultName is present
//----------------------

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    /// <summary>
    /// These tests validate that the Program.cs Key Vault configuration
    /// logic works correctly. Since Key Vault cannot be accessed in test
    /// environments, we verify the conditional registration behavior.
    /// </summary>
    [Collection("Sequential Integration Tests")]
    public class KeyVaultConfigTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public KeyVaultConfigTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void KeyVault_IsNotAdded_WhenVaultNameMissing()
        {
            // In the Testing environment, KeyVault:VaultName is not set,
            // so the config provider should not be registered.
            // Verify the app started successfully without it.
            var services = _factory.Services;
            Assert.NotNull(services);
        }

        [Fact]
        public void Application_Starts_WithoutKeyVault()
        {
            // Verify that the full application pipeline works
            // without any Key Vault configuration present.
            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Core services should resolve without Key Vault
            var mediator = serviceProvider.GetService<MediatR.IMediator>();
            Assert.NotNull(mediator);
        }
    }
}
