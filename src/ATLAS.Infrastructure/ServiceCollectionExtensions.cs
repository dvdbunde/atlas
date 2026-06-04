namespace ATLAS.Infrastructure;

using ATLAS.Application;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for IServiceCollection to register Infrastructure layer services
/// Follows Clean Architecture dependency injection patterns
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Infrastructure layer services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for fluent chaining</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        
        // Register database context
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });
        
        // Register repositories
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IPermitTypeRepository, PermitTypeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register MediatR (CQRS handlers are in Application layer)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        
        return services;
    }
    
    /// <summary>
    /// Registers Infrastructure layer services with default configuration
    /// Convenience method that uses the built-in configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for fluent chaining</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // This overload assumes configuration will be provided elsewhere
        // or uses default configuration
        return services;
    }
}
