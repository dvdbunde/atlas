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
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
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

            // Bind Email configuration to strongly-typed options.
            services.AddOptions<EmailOptions>()
                .Bind(configuration.GetSection(EmailOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Email delivery: production uses Azure Communication Services (ACS) with Managed
            // Identity; the local email sink is development-only.
            services.AddTransient<IEmailService>(sp =>
            {
                var emailOptions = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<AcsEmailService>>();
                var isDevelopment = IsDevelopmentEnvironment(sp.GetService<IHostEnvironment>(), configuration);

                if (!string.IsNullOrWhiteSpace(emailOptions.Acs.Endpoint)
                    && !string.IsNullOrWhiteSpace(emailOptions.Acs.SenderAddress))
                {
                    var acsLogger = sp.GetRequiredService<ILogger<AcsEmailClient>>();
                    var emailClient = new AcsEmailClient(new Uri(emailOptions.Acs.Endpoint), acsLogger);
                    return new AcsEmailService(emailClient, sp.GetRequiredService<IOptions<EmailOptions>>(), logger);
                }

                // ACS is not configured. The local sink is development-only; in a deployed
                // environment, missing ACS configuration must fail fast rather than silently
                // sending nothing or falling back to a development sink.
                if (!isDevelopment)
                {
                    throw new InvalidOperationException(
                        "Azure Communication Services email is not configured. " +
                        "Set Email:Acs:Endpoint and Email:Acs:SenderAddress in a non-development environment.");
                }

                return new LocalEmailService(sp.GetRequiredService<ILogger<LocalEmailService>>());
            });
            services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();          
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

            // Register a BlobServiceClient when Blob Storage is configured.
            // Precedence:
            //   1. Storage:AccountName          -> Azure using DefaultAzureCredential (Managed Identity)
            //   2. Storage:ConnectionString     -> local Azurite / any valid Blob connection string
            //   3. Neither                      -> no client registered; consumers fall back to
            //                                     source-code defaults / default-only behavior.
            // The connection string is the local-development fallback and is never selected when a
            // valid Azure account name is configured (production stays Managed Identity / RBAC based).
            var storageAccountName = configuration["Storage:AccountName"];
            var storageConnectionString = configuration["Storage:ConnectionString"];

            if (!string.IsNullOrWhiteSpace(storageAccountName))
            {
                services.AddAzureClients(clientBuilder =>
                {
                    clientBuilder.AddBlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"));
                    clientBuilder.UseCredential(new DefaultAzureCredential());
                });
            }
            else if (!string.IsNullOrWhiteSpace(storageConnectionString))
            {
                // Local development (Azurite): construct the Blob Service client from the
                // configured connection string. Supports UseDevelopmentStorage=true and any
                // other valid Blob Storage connection string.
                services.AddSingleton(new BlobServiceClient(storageConnectionString));
            }

            // Register BlobStorageService (supports connection-string and managed-identity)
            services.AddScoped<IFileStorageService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<StorageOptions>>();
                var blobServiceClient = sp.GetService<BlobServiceClient>();
                return new BlobStorageService(options, blobServiceClient);
            });

            // Blob-backed email template store. The blob client is optional: it is only
            // present when a BlobServiceClient is configured (production via Managed
            // Identity, or local Azurite). When absent, BlobEmailTemplateStore serves
            // the source-code defaults and local development needs no Azure dependency.
            services.AddScoped<IEmailTemplateStore>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<BlobEmailTemplateStore>>();

                var blobServiceClient = sp.GetService<BlobServiceClient>();
                IEmailTemplateBlobClient? blobClient = null;
                if (blobServiceClient is not null)
                {
                    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
                    var adapterLogger = sp.GetRequiredService<ILogger<AzureBlobEmailTemplateClient>>();
                    blobClient = new AzureBlobEmailTemplateClient(
                        blobServiceClient, options.EmailTemplatesContainer, adapterLogger);
                }

                return new BlobEmailTemplateStore(blobClient, logger);
            });

            // Register virus scanner (pass-through for MVP)
            services.AddScoped<IVirusScanner, PassThroughVirusScanner>();

            return services;
        }

        /// <summary>
        /// Determines whether the application is running in a development environment.
        /// Falls back to the ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT configuration value
        /// when no IHostEnvironment is resolvable (e.g. in unit-test registrations).
        /// </summary>
        private static bool IsDevelopmentEnvironment(IHostEnvironment? environment, IConfiguration configuration)
        {
            if (environment is not null)
                return environment.IsDevelopment();

            var envName = configuration["ASPNETCORE_ENVIRONMENT"]
                ?? configuration["DOTNET_ENVIRONMENT"]
                ?? string.Empty;

            return string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}
