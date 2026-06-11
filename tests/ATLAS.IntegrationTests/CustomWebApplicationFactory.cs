using ATLAS.Application.Behaviors;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using ATLAS.Infrastructure.Services;
using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATLAS.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing so Program.cs skips SQL Server registration
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // This runs AFTER Program.cs, so we can override here
            // Remove ALL DbContext-related registrations to avoid double registration
            var descriptors = services.Where(
                d => d.ServiceType == typeof(ApplicationDbContext) ||
                     d.ImplementationType == typeof(ApplicationDbContext) ||
                     d.ServiceType.Name.Contains("DbContext") ||
                     d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();
            
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("ATLAS_Test_DB");
            });


            // Call a test-specific infrastructure registration
            services.AddInfrastructureForTesting();
        
            // Register test authentication scheme — no Entra ID, no JWT, no external deps
            // TestAuthHandler reads identity from the X-Test-Identity header sent by TestHttpContextExtensions
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthDefaults.AuthenticationScheme;
                options.DefaultScheme = TestAuthDefaults.AuthenticationScheme;
            })
            .AddScheme<TestAuthenticationOptions, TestAuthHandler>(
                TestAuthDefaults.AuthenticationScheme, options => { });

            // Keep REAL authorization — test authentication provides role claims
            // that policies evaluate against. No bypass.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Authenticated", policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy("Citizen", policy =>
                    policy.RequireRole("Citizen"));
                options.AddPolicy("Officer", policy =>
                    policy.RequireRole("Officer"));
                options.AddPolicy("Admin", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("OfficerOrAdmin", policy =>
                    policy.RequireRole("Officer", "Admin"));
            });

            // Register test claims transformation
            services.AddScoped<IClaimsTransformation, TestClaimsTransformation>();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
        });

        // Enable detailed logging
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore", "Debug");
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore.Routing", "Trace");
        builder.UseSetting("Logging:Console:IncludeScopes", "true");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed test data
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if data already exists
            if (!context.PermitTypes.Any())
            {
                SeedTestData(context);
            }
        }

        return host;
    }

    private void SeedTestData(ApplicationDbContext context)
    {
        // Seed PermitTypes (IsActive = true by default)
        var buildingPermit = new PermitType("Building Permit", "For construction and renovations", 150.00m);
        var eventPermit = new PermitType("Event Permit", "For public events and gatherings", 75.00m);
        var signagePermit = new PermitType("Signage Permit", "For temporary signage", 25.00m);
        
        context.PermitTypes.AddRange(new[] { buildingPermit, eventPermit, signagePermit });

        // Seed Users
        var citizen = new User("citizen@test.com", "John", "Doe", UserRole.Citizen);
        var officer = new User("officer@test.com", "Jane", "Smith", UserRole.Officer);
        var admin = new User("admin@test.com", "Admin", "User", UserRole.Admin);
        
        context.Users.AddRange(new[] { citizen, officer, admin });
        
        // Save to get IDs
        context.SaveChanges();

        // Store seeded IDs for tests to use
        TestData.BuildingPermitTypeId = buildingPermit.Id;
        TestData.EventPermitTypeId = eventPermit.Id;
        TestData.SignagePermitTypeId = signagePermit.Id;
        TestData.CitizenUserId = citizen.Id;
        TestData.OfficerUserId = officer.Id;
        TestData.AdminUserId = admin.Id;

        // Seed Applications (using seeded users and permit types)
        var application1 = new  ATLAS.Domain.Entities.Application(citizen.Id, buildingPermit.Id, "Initial application for home renovation");
        application1.AddDocument(Guid.NewGuid(),"building_plan.pdf", "application/pdf", 2048, "https://blob.test.com/building_plan.pdf", citizen.Id);
        application1.Submit();
        application1.StartReview(officer.Id);
        //application1.Approve(officer.Id, "Approved - meets all requirements");
        
        var application2 = new  ATLAS.Domain.Entities.Application(citizen.Id, eventPermit.Id, "Annual community event permit");
        application2.AddDocument(Guid.NewGuid(), "event_layout.pdf", "application/pdf", 3072, "https://blob.test.com/event_layout.pdf", citizen.Id);    
        application2.Submit();
        
        var application3 = new ATLAS.Domain.Entities.Application(citizen.Id, signagePermit.Id, "Temporary signage for business");
        application3.AddDocument(Guid.NewGuid(), "signage_design.pdf", "application/pdf", 1024, "https://blob.test.com/signage_design.pdf", citizen.Id);    
        
        context.Applications.AddRange(new[] { application1, application2, application3 });
        context.SaveChanges();

        // Store application IDs
        TestData.Application1Id = application1.Id;
        TestData.Application2Id = application2.Id;
        TestData.Application3Id = application3.Id;       
        
        context.SaveChanges();

        // Store document IDs
        TestData.Document1Id = application1.Documents[0].Id;
        TestData.Document2Id = application2.Documents[0].Id;
        TestData.Document3Id = application3.Documents[0].Id;

        // Seed Audit Logs
        var audit1 = new AuditLog(citizen.Id, "ApplicationSubmitted", "Application", application1.Id, "Application submitted", "127.0.0.1");
        var audit2 = new AuditLog(officer.Id, "ApplicationApproved", "Application", application1.Id, "Application approved", "127.0.0.1");
        var audit3 = new AuditLog(citizen.Id, "DocumentUploaded", "Document", TestData.Document1Id, "Document uploaded", "127.0.0.1");
        
        context.AuditLogs.AddRange(new[] { audit1, audit2, audit3 });
        
        context.SaveChanges();
    }
}

// Static class to expose seeded IDs to tests
public static class TestData
{
    public static Guid BuildingPermitTypeId { get; set; }
    public static Guid EventPermitTypeId { get; set; }
    public static Guid SignagePermitTypeId { get; set; }
    public static Guid CitizenUserId { get; set; }
    public static Guid OfficerUserId { get; set; }
    public static Guid AdminUserId { get; set; }
    public static Guid Application1Id { get; set; }
    public static Guid Application2Id { get; set; }
    public static Guid Application3Id { get; set; }
    public static Guid Document1Id { get; set; }
    public static Guid Document2Id { get; set; }
    public static Guid Document3Id { get; set; }
}

public static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IPermitTypeRepository, PermitTypeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityResolver, IdentityResolver>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ATLAS.Application.AssemblyMarker).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ATLAS.Infrastructure.AssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(UserSynchronizationBehavior<,>));
        });
        return services;
    }
}
