using ATLAS.Application.Queries.Documents;
using ATLAS.Blazor.Components;
using ATLAS.Infrastructure;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Data.SeedData;
using Azure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// O3: OpenTelemetry tracing for Blazor-initiated operations.
// Registers the ATLAS application ActivitySource so command Activities from
// TracingBehavior are collected.
//
// O4: OpenTelemetry metrics for ATLAS business metrics.
//
// Azure Monitor export is only enabled when an Application Insights
// connection string is configured. Locally (no connection string),
// telemetry remains local/no-op and does not require Azure Monitor.
var appInsightsConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

var openTelemetry = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("ATLAS.Blazor"))
    .WithTracing(tracing =>
    {
        tracing.AddSource(
            ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName);
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(
            ATLAS.Application.Telemetry.AtlasMetrics.MeterName);
    });

if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    openTelemetry.UseAzureMonitor(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

// Azure Key Vault configuration provider (production only)
var keyVaultName = builder.Configuration["KeyVault:VaultName"];
Uri? keyVaultUri = null;
if (!string.IsNullOrWhiteSpace(keyVaultName))
{
    keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

// Health checks
var healthChecks = builder.Services.AddHealthChecks();

var storageAccountName = builder.Configuration["Storage:AccountName"];

if (!builder.Environment.IsDevelopment() &&
    builder.Environment.EnvironmentName != "Testing" &&
    !string.IsNullOrWhiteSpace(storageAccountName))
{
    var storageUri = new Uri(
        $"https://{storageAccountName}.blob.core.windows.net");

    var blobServiceClient = new BlobServiceClient(
        storageUri,
        new DefaultAzureCredential());

    healthChecks.AddCheck(
        "permit-documents-storage",
        new BlobContainerHealthCheck(
            blobServiceClient,
            "permit-documents"),
        tags: ["storage", "azure"]);

    healthChecks.AddCheck(
        "email-templates-storage",
        new BlobContainerHealthCheck(
            blobServiceClient,
            "email-templates"),
        tags: ["storage", "azure"]);
}

if (keyVaultUri != null)
{
    healthChecks.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential(),
         options =>
        {
            options.AddSecret("sql-connection-string");
        },
        name: "key-vault", tags: ["secrets", "azure"]);
}

healthChecks.AddDbContextCheck<ApplicationDbContext>(
    name: "database",
    tags: ["database", "critical"]);


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure OpenID Connect authentication via Microsoft.Identity.Web
// Reusing the EXISTING ATLAS-API app registration — no new Entra app.
var azureAd = builder.Configuration.GetSection("AzureAd");
if (string.IsNullOrWhiteSpace(azureAd["TenantId"]))
    throw new InvalidOperationException("AzureAd:TenantId is required");
if (string.IsNullOrWhiteSpace(azureAd["ClientId"]))
    throw new InvalidOperationException("AzureAd:ClientId is required");


builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(azureAd);

// Add authorization services
builder.Services.AddAuthorization();

// Add Blazor-specific authentication/authorization support
builder.Services.AddCascadingAuthenticationState();

// Register Infrastructure layer (CurrentUserService, IExecutionContext, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Register MediatR — scan Application and Infrastructure assemblies for handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ATLAS.Application.AssemblyMarker).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ATLAS.Infrastructure.Services.CurrentUserService).Assembly);

    // Add pipeline behaviors
    cfg.AddOpenBehavior(typeof(ATLAS.Application.Behaviors.ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ATLAS.Application.Behaviors.UserSynchronizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ATLAS.Application.Behaviors.TransactionBehavior<,>));
    // O3: opens a W3C Activity per command (Blazor operation boundary).
    cfg.AddOpenBehavior(typeof(ATLAS.Application.Behaviors.TracingBehavior<,>));
});

// Register UI pages for Microsoft.Identity.Web login/logout
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
    
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed permit type data on startup (skip in Testing environment - test factory seeds its own data)
if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var seedDataLoader = scope.ServiceProvider.GetRequiredService<SeedDataLoader>();
        await seedDataLoader.LoadSeedDataAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // Authenticate using OpenID Connect cookies
app.UseAuthorization();    // Enforce authorization policies

// Map health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(
            new { status = report.Status.ToString(), totalDuration = report.TotalDuration.TotalMilliseconds }));
    }
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database") || check.Tags.Contains("storage") || check.Tags.Contains("secrets"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(
            new { status = report.Status.ToString(), totalDuration = report.TotalDuration.TotalMilliseconds }));
    }
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/documents/{documentId:guid}/download", async (
    Guid documentId,
    IMediator mediator,
    IWebHostEnvironment env) =>
{
    var query = new DownloadDocumentQuery { DocumentId = documentId };
    var result = await mediator.Send(query);

    if (result == null)
        return Results.NotFound();

    // Development (Azurite): proxy through server to avoid CORS.
    // Production (Azure Storage): 302 redirect for direct client download.
    if (env.IsDevelopment())
    {
        using var httpClient = new HttpClient();
        using var blobResponse = await httpClient.GetAsync(result.SasUri);

        if (!blobResponse.IsSuccessStatusCode)
            return Results.Problem("Failed to retrieve document from storage.",
                statusCode: (int)blobResponse.StatusCode);

        var contentBytes = await blobResponse.Content.ReadAsByteArrayAsync();
        return Results.File(contentBytes, result.ContentType, result.FileName);
    }

    return Results.Redirect(result.SasUri);
});

// Map login/logout endpoints from Microsoft.Identity.Web.UI
app.MapRazorPages();
app.MapControllers();

app.Run();


public sealed class BlobContainerHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobContainerHealthCheck(
        BlobServiceClient blobServiceClient,
        string containerName)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = containerName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient =
                _blobServiceClient.GetBlobContainerClient(_containerName);

            var exists = await containerClient.ExistsAsync(cancellationToken);

            if (!exists.Value)
            {
                return HealthCheckResult.Unhealthy(
                    $"Blob container '{_containerName}' does not exist.");
            }

            return HealthCheckResult.Healthy(
                $"Blob container '{_containerName}' is accessible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Blob container '{_containerName}' is not accessible.",
                ex);
        }
    }
}