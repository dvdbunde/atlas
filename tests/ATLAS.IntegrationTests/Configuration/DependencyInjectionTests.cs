using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration;

public class DependencyInjectionTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    public DependencyInjectionTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public void API_Should_Register_MediatR()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        // Act & Assert - This will fail because MediatR is not registered yet
        var mediator = services.GetService<MediatR.IMediator>();
        Assert.NotNull(mediator);
    }
}
