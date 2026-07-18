using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ATLAS.API.Middleware
{
    // Custom class that extends ProblemDetails with Errors property
    public class ValidationProblemDetails : ProblemDetails
    {
        public System.Collections.Generic.Dictionary<string, string[]> Errors { get; set; } = new();
    }

    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        
        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
        
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            object response;
            
            // Map domain exceptions to 400 Bad Request
            if (exception is ATLAS.Domain.DomainException)
            {
                statusCode = HttpStatusCode.BadRequest;
                response = new ProblemDetails
                {
                    Title = "Domain Validation Error",
                    Status = (int)statusCode,
                    Detail = exception.Message
                };
            }
            // Map validation exceptions to 400 Bad Request
            else if (exception is FluentValidation.ValidationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                var validationEx = (FluentValidation.ValidationException)exception;
                response = new ValidationProblemDetails
                {
                    Title = "Validation Failed",
                    Status = (int)statusCode,
                    Detail = "One or more validation errors occurred",
                    Errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                };
            }
            else if (exception is ATLAS.Application.Behaviors.ValidationException appValidationEx)
            {
                statusCode = HttpStatusCode.BadRequest;
                response = new ValidationProblemDetails
                {
                    Title = "Validation Failed",
                    Status = (int)statusCode,
                    Detail = "One or more validation errors occurred",
                    Errors = appValidationEx.Failures
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                };
            }
            // Map unauthorized access to 401
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                response = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = (int)statusCode,
                    Detail = exception.Message
                };
            }
            // Map not found to 404
            else if (exception is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                response = new ProblemDetails
                {
                    Title = "Resource Not Found",
                    Status = (int)statusCode,
                    Detail = exception.Message
                };
            }
            // Unhandled exceptions → 500 (with detailed error in Development)
            else
            {
                _logger.LogError(exception, "Unhandled exception");
                
                // In Development, show the actual exception details
                var detail = "An unexpected error occurred. Please contact support.";
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
                context.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName == "Testing")
                {
                    detail = $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
                }
                
                response = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = (int)statusCode,
                    Detail = detail
                };
            }
            
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}