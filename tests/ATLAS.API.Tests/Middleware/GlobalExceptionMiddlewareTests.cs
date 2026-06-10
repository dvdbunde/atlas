using ATLAS.API.Contracts.Generated;
using ATLAS.API.Middleware;
using ATLAS.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ATLAS.API.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
        private readonly DefaultHttpContext _httpContext;

        public GlobalExceptionMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();

            // Mock the service provider to avoid null ref in generic handler
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(s => s.GetService(typeof(IWebHostEnvironment)))
                .Returns(mockEnvironment.Object); // Environment check will fall through safely
            _httpContext.RequestServices = mockServiceProvider.Object;
        }

        [Fact]
        public async Task InvokeAsync_NoException_ShouldCallNext()
        {
            // Arrange
            var nextCalled = false;
            var middleware = new GlobalExceptionMiddleware(
                (context) =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_DomainException_ShouldReturnBadRequest()
        {
            // Arrange
            var middleware = new GlobalExceptionMiddleware(
                (context) => throw new DomainException("Test error"),
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal(400, _httpContext.Response.StatusCode);
            _httpContext.Response.Body.Position = 0;
            using var reader = new StreamReader(_httpContext.Response.Body);
            var response = JsonSerializer.Deserialize<ProblemDetails>(
                await reader.ReadToEndAsync());
            Assert.Equal("Domain Validation Error", response!.Title);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_ShouldReturnInternalServerError()
        {
            // Arrange
            var middleware = new GlobalExceptionMiddleware(
                (context) => throw new Exception("Test error"),
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal(500, _httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_ShouldReturnNotFound()
        {
            // Arrange
            var middleware = new GlobalExceptionMiddleware(
                (context) => throw new KeyNotFoundException("Resource not found"),
                _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal(404, _httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task Constructor_NullNextDelegate_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GlobalExceptionMiddleware(null!, _mockLogger.Object));
        }
    }  
}
