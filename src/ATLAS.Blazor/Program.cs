using ATLAS.Blazor.Components;
using ATLAS.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

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
});

// Register UI pages for Microsoft.Identity.Web login/logout
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
    
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // Authenticate using OpenID Connect cookies
app.UseAuthorization();    // Enforce authorization policies

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map login/logout endpoints from Microsoft.Identity.Web.UI
app.MapRazorPages();
app.MapControllers();

app.Run();