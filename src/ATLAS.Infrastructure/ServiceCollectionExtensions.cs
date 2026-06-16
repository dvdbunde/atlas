namespace ATLAS.Infrastructure
{
    using ATLAS.Application;
    using ATLAS.Application.Behaviors;
    using ATLAS.Application.Interfaces;
    using ATLAS.Domain.Entities;
    using ATLAS.Domain.Events;
    using ATLAS.Domain.Interfaces;
    using ATLAS.Infrastructure.Data;
    using ATLAS.Infrastructure.Data.SeedData;
    using ATLAS.Infrastructure.EventHandlers;
    using ATLAS.Infrastructure.Repositories;
    using ATLAS.Infrastructure.Services;
    using FluentValidation;
    using MediatR;
    using Microsoft.AspNetCore.Http;
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

            // Register HTTP context accessor for identity resolution
            services.AddHttpContextAccessor();

            // Register current user service — resolves authenticated user identity from HTTP context
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Register identity resolver — synchronizes claims with Domain User aggregate
            services.AddScoped<IIdentityResolver, IdentityResolver>();

            // Register execution context — request-scoped identity + correlation tracing
            services.AddScoped<IExecutionContext, ExecutionContext>();

            // Register repositories
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IPermitTypeRepository, PermitTypeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            
            // Register seed data loader
            services.AddScoped<SeedDataLoader>();

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Register MediatR - scan both Application and Infrastructure assemblies
            services.AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly); // Application layer
                cfg.RegisterServicesFromAssembly(typeof(AuditLogRepository).Assembly); // Infrastructure layer (for event handlers)

                // ✅ Add validation pipeline behavior
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                
                // User synchronization pipeline behavior — runs before every request handler
                cfg.AddOpenBehavior(typeof(UserSynchronizationBehavior<,>));

            });
            
            return services;
        }
        
        /// <summary>
        /// Registers Infrastructure layer services with default configuration.
        /// Convenience overload for environments where full configuration isn't available
        /// (e.g., tests). Still registers HTTP context accessor and current user service.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for fluent chaining</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IExecutionContext, ExecutionContext>();

            //----------------------
            // Email Services (Phase E1)
            //----------------------

            // Email service (SMTP for development)
            services.AddTransient<IEmailService, SmtpEmailService>();

            // Email template renderer
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

            // Email event handlers (MediatR auto-discovers, but explicit for clarity)
            services.AddScoped<INotificationHandler<ApplicationSubmittedEvent>, ApplicationSubmittedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationApprovedEvent>, ApplicationApprovedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationRejectedEvent>, ApplicationRejectedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationInfoRequestedEvent>, ApplicationInfoRequestedEmailHandler>();

            return services;
        }
    }
}
