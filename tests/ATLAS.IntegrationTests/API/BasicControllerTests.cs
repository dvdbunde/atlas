using ATLAS.Domain.Interfaces;
using ATLAS.IntegrationTests.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace ATLAS.IntegrationTests.API
{
    public class BasicControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output; // 🆕 Add this

        public BasicControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _factory = factory;
            _output = output;
        }

        [Fact]
        public async Task VerifyServicesRegistered()
        {            
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetService<IMediator>();
            var repo = scope.ServiceProvider.GetService<IApplicationRepository>();
            
            Assert.NotNull(mediator);
            Assert.NotNull(repo); // This will fail with current setup
        }     

        [Fact]
        public void VerifyControllersDiscovered()
        {
            // Arrange            
            var controllerTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Controller"))
                .ToList();

            // Act & Assert
            Assert.NotEmpty(controllerTypes);
            foreach (var controller in controllerTypes)
            {
                _output.WriteLine($"✅ Found controller: {controller.Name}");
            }
        }

        [Fact]
        public async Task DebugTest()
        {
            _output.WriteLine("=== DEBUG TEST START ===");
            
            // Check 1: Can we create a scope?
            using var scope = _factory.Services.CreateScope();
            _output.WriteLine("✅ Scope created");
            
            // Check 2: Are controllers in the assembly?
            var controllerTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Controller"))
                .ToList();
            _output.WriteLine($"✅ Found {controllerTypes.Count} controllers:");
            foreach (var ct in controllerTypes)
            {
                _output.WriteLine($"   - {ct.Name}");
            }
            
            // Check 3: Try a simple GET
            _output.WriteLine("📡 Making request to /api/applications...");
            var response = await _client.GetAsAsync("/api/applications", TestUserBuilder.AsAdmin());
            _output.WriteLine($"📡 Response status: {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"📡 Response content: {content}");
            
            // Check 4: What endpoints are registered?            
            var endpointDataSource = scope.ServiceProvider.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
            if (endpointDataSource != null)
            {
                _output.WriteLine("📍 Registered endpoints:");
                foreach (var endpoint in endpointDataSource.Endpoints)
                {
                    _output.WriteLine($"   - {endpoint.DisplayName}");
                }
            }
            else
            {
                _output.WriteLine("❌ No EndpointDataSource found");
            }
            
            _output.WriteLine("=== DEBUG TEST END ===");
        }
    }
}
