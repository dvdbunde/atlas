namespace ATLAS.Infrastructure;

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
        
        // Register database context (when implemented)
        // services.AddDbContext<ApplicationDbContext>(options =>
        // {
        //     options.UseSqlServer(
        //         configuration.GetConnectionString("DefaultConnection"),
        //         sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        // });
        
        // Register repositories (when implemented)
        // services.AddScoped<IApplicationRepository, ApplicationRepository>();
        // services.AddScoped<IPermitTypeRepository, PermitTypeRepository>();
        // services.AddScoped<IDocumentRepository, DocumentRepository>();
        // services.AddScoped<IReviewRepository, ReviewRepository>();
        
        // Register Unit of Work (when implemented)
        // services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register external service clients (when implemented)
        // services.AddHttpClient<ISomeExternalService, SomeExternalService>();
        
        // Register caching (when implemented)
        // services.AddMemoryCache();
        // services.AddDistributedMemoryCache();
        
        // Register background services (when implemented)
        // services.AddHostedService<SomeBackgroundService>();
        
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