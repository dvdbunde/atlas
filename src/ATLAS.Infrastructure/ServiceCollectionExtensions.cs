namespace ATLAS.Infrastructure
{
    using ATLAS.Application;
    using ATLAS.Application.Behaviors;
    using ATLAS.Application.EmailTemplates;
    using ATLAS.Application.Interfaces;
    using ATLAS.Domain.Entities;
    using ATLAS.Domain.Events;
    using ATLAS.Domain.Interfaces;
    using ATLAS.Infrastructure.Data;
    using ATLAS.Infrastructure.Data.SeedData;
    using ATLAS.Infrastructure.EventHandlers;
    using ATLAS.Infrastructure.Repositories;
    using ATLAS.Infrastructure.Options;
    using ATLAS.Infrastructure.Services;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using FluentValidation;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using ATLAS.Infrastructure.EmailTemplates;
    using Microsoft.Extensions.Azure;

    /// <summary>
    /// Extension methods for IServiceCollection to register Infrastructure layer services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Infrastructure layer services with the dependency injection container
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Register database context with retry for Azure SQL transient faults
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IIdentityResolver, IdentityResolver>();
            services.AddScoped<IExecutionContext, ExecutionContext>();

            services.AddTransient<IEmailService, SmtpEmailService>();
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
            services.AddScoped<IEmailTemplateStore, FileEmailTemplateStore>();
            services.AddScoped<INotificationHandler<ApplicationSubmittedEvent>, ApplicationSubmittedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationApprovedEvent>, ApplicationApprovedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationRejectedEvent>, ApplicationRejectedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationInfoRequestedEvent>, ApplicationInfoRequestedEmailHandler>();
            services.AddScoped<INotificationHandler<ApplicationResubmittedEvent>, ApplicationResubmittedEmailHandler>();

            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IPermitTypeRepository, PermitTypeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<SeedDataLoader>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Bind Storage configuration to strongly-typed options
            services.AddOptions<StorageOptions>()
                .Bind(configuration.GetSection(StorageOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register blob storage client using DefaultAzureCredential
            var storageAccountName = configuration["Storage:AccountName"];
            if (!string.IsNullOrWhiteSpace(storageAccountName))
            {
                services.AddAzureClients(clientBuilder =>
                {
                    clientBuilder.AddBlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"));
                    clientBuilder.UseCredential(new DefaultAzureCredential());
                });
            }

            // Register BlobStorageService (supports connection-string and managed-identity)
            services.AddScoped<IFileStorageService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<StorageOptions>>();
                var blobServiceClient = sp.GetService<BlobServiceClient>();
                return new BlobStorageService(options, blobServiceClient);
            });

            // Register virus scanner (pass-through for MVP)
            services.AddScoped<IVirusScanner, PassThroughVirusScanner>();

            return services;
        }
    }
}
