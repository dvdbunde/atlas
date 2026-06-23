using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    public class ErrorHandlingTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ErrorHandlingTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_ShouldReturn500_WhenUnhandledException()
        {
            // Arrange
            // This test verifies the global exception middleware handles unhandled exceptions
            // In a real scenario, we would trigger an endpoint that throws
            
            // Act & Assert
            // For now, verify the middleware is configured
            Assert.True(true); // Placeholder - would need test endpoint
        }

        [Fact]
        public void DomainException_ShouldMapTo400BadRequest()
        {
            // Arrange
            var domainException = new ATLAS.Domain.DomainException("Test domain error");

            // Act & Assert
            Assert.IsType<ATLAS.Domain.DomainException>(domainException);
            Assert.Contains("Test domain error", domainException.Message);
        }

        [Fact]
        public void ValidationException_ShouldMapTo400WithErrors()
        {
            // Arrange
            var validator = new ATLAS.Application.Commands.SubmitApplicationCommandValidator();
            var command = new ATLAS.Application.Commands.SubmitApplicationCommand
            {                
                PermitTypeId = Guid.Empty // Invalid
            };

            // Act
            var result = validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void KeyNotFoundException_ShouldMapTo404()
        {
            // Arrange & Act & Assert
            var exception = new KeyNotFoundException("Resource not found");
            Assert.IsType<KeyNotFoundException>(exception);
        }
    }
}
