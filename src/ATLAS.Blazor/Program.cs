using ATLAS.Blazor.Components;
using ATLAS.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Register Infrastructure layer (DbContext, repositories, MediatR)
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddInfrastructure(builder.Configuration);
}
else
{
    // Test environment uses the no-config overload
    builder.Services.AddInfrastructure();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();