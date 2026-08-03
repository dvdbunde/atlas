//----------------------
// Application Insights Tests
// Verifies that App Insights telemetry is not registered in Testing environment
//----------------------

using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    [Collection("Sequential Integration Tests")]
    public class ApplicationInsightsTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ApplicationInsightsTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void ApplicationInsights_NotRegistered_InTestingEnvironment()
        {
            // In the "Testing" environment, Application Insights is skipped.
            // TelemetryClient should NOT be resolvable.
            var client = _factory.Services.GetService<TelemetryClient>();
            Assert.Null(client);
        }

        [Fact]
        public void Application_Starts_WithoutApplicationInsights()
        {
            // Verify core services work without Application Insights
            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var mediator = serviceProvider.GetService<MediatR.IMediator>();
            Assert.NotNull(mediator);
        }
    }
}
