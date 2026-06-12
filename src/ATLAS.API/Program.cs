using System.Diagnostics;
using System.Security.Claims;
using ATLAS.API.Auth;
using ATLAS.API.Controllers;
using ATLAS.Application.Behaviors;
using ATLAS.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Only register SQL Server if not in test environment (tests use InMemory)
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddInfrastructure(builder.Configuration);
}
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);    
    cfg.RegisterServicesFromAssembly(typeof(ATLAS.Application.AssemblyMarker).Assembly);
    
    // User synchronization pipeline behavior — runs before every request handler
    cfg.AddOpenBehavior(typeof(UserSynchronizationBehavior<,>));
});

// Register FluentValidation validators from Application layer
builder.Services.AddValidatorsFromAssembly(typeof(ATLAS.Application.AssemblyMarker).Assembly);

// Read Azure AD config for Swagger OAuth2 and JWT validation
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
var tenantId = azureAdConfig["TenantId"] ?? "common";
var clientId = azureAdConfig["ClientId"] ?? "";
var swaggerClientId = azureAdConfig["SwaggerClientId"] ?? "";

if (builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(swaggerClientId))
    {
        throw new InvalidOperationException(
            "AzureAd:SwaggerClientId is required");
    }
}

var swaggerScope = $"api://{clientId}/atlas.access";

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ATLAS API", 
        Version = "v1" 
    });

    // Entra ID OAuth2 Authorization Code security definition for Swagger UI "Authorize" button
    var authorizationUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
    var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

    c.AddSecurityDefinition("EntraID", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(authorizationUrl),
                TokenUrl = new Uri(tokenUrl),
                Scopes = new Dictionary<string, string>
                {
                    { swaggerScope, "Access ATLAS API" }
                }
            }
        },
        Description = "Microsoft Entra ID OAuth2 Authorization Code flow. Obtain a token from Entra ID and use it as a Bearer token in API requests."
    });

    // Apply globally so all endpoints show the padlock
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "EntraID"
                }
            },
            new[] { swaggerScope }
        }
    });
});

builder.Services.AddControllers(options =>
{
    // Apply authorization policies to NSwag-generated controllers based on OpenAPI spec
    options.Conventions.Add(new GeneratedControllerAuthorizationConvention());
});

// Configure JWT Bearer authentication for Microsoft Entra ID
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var azureAd = builder.Configuration.GetSection("AzureAd");
        var instance = azureAd["Instance"] ?? "https://login.microsoftonline.com";
        var tenantId = azureAd["TenantId"];
        var audience = azureAd["Audience"];

        if (string.IsNullOrWhiteSpace(tenantId))
            throw new InvalidOperationException("AzureAd:TenantId is required. Verify appsettings.json / Azure Key Vault.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("AzureAd:Audience is required. Verify appsettings.json / Azure Key Vault.");

        
        options.Authority = $"{instance}/{tenantId}/v2.0";
        //options.Audience = audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,            
            ValidIssuers =
            [
                $"https://sts.windows.net/{tenantId}/",                    // v1.0
                $"https://login.microsoftonline.com/{tenantId}/v2.0"       // v2.0
            ],
            ValidateAudience = true,
            ValidAudiences =
            [
                clientId,                                                  // v2.0 format
                $"api://{clientId}",                                       // v1.0 format
                audience                                                   // from config (backward compat)
            ],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        // Map inbound claims to standard claim types (sub -> ClaimTypes.NameIdentifier)
        options.MapInboundClaims = true;
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "JWT authentication failed: {ErrorMsg} | Path: {Path} | Headers: {Headers}",
                    context.Exception?.Message,
                    context.HttpContext.Request.Path,
                    context.HttpContext.Request.Headers.ContainsKey("Authorization") ? "Present" : "Missing");
                await Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation(
                    "JWT token validated | Sub: {Sub} | Iss: {Iss}",
                    context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    context.Principal?.FindFirst("iss")?.Value);
                await Task.CompletedTask;
            }
        };    });

builder.Services.AddAuthorization(options =>
{
    // Policy: any authenticated user (Citizen, Officer, or Admin)
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Policy: Citizen role only
    options.AddPolicy("Citizen", policy =>
        policy.RequireRole("Citizen"));

    // Policy: Officer role only
    options.AddPolicy("Officer", policy =>
        policy.RequireRole("Officer"));

    // Policy: Admin role only
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));

    // Policy: Officer or Admin (e.g., read-only admin views)
    options.AddPolicy("OfficerOrAdmin", policy =>
        policy.RequireRole("Officer", "Admin"));
});

// Claims transformation: map Entra ID roles/groups → application role claims
builder.Services.AddScoped<IClaimsTransformation, AtlasClaimsTransformation>();

// CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.WithOrigins("https://localhost:7295")
                      .AllowAnyMethod()
                      .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.EnvironmentName == "Testing" || app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ATLAS API v1");
        options.OAuthClientId(swaggerClientId);
        options.OAuthScopes(swaggerScope);
        options.OAuthUsePkce();
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthentication();  // Authenticate JWT Bearer tokens
app.UseAuthorization();   // Enforce authorization policies
app.UseMiddleware<ATLAS.API.Middleware.GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }