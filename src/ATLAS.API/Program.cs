using System.Diagnostics;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Infrastructure;
using ATLAS.Infrastructure;
using FluentValidation;
using Microsoft.OpenApi;

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
});

// Register FluentValidation validators from Application layer
builder.Services.AddValidatorsFromAssembly(typeof(ATLAS.Application.AssemblyMarker).Assembly);

// 🚨 NEW: Authorization conventions for generated controllers
/*
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new GeneratedControllerAuthorizationConvention());
});*/


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "ATLAS API", 
        Version = "v1" 
    });
});

builder.Services.AddControllers();


/*
builder.Services.AddScoped<IApplicationsController, ApplicationsController>();
builder.Services.AddScoped<IDocumentsController, DocumentsController>();
builder.Services.AddScoped<IPermitTypesController, PermitTypesController>();
builder.Services.AddScoped<IUsersController, UsersController>();
builder.Services.AddScoped<IAuditLogsController, AuditLogsController>();
*/

// 🚨 NEW: Authentication/Authorization
// TODO: Configure JWT Bearer authentication for Entra ID
// builder.Services.AddAuthentication().AddJwtBearer(); // Requires Microsoft.AspNetCore.Authentication.JwtBearer package
//builder.Services.AddAuthentication(); // Placeholder - configure when Entra ID is set up
//builder.Services.AddAuthorization();

// 🚨 NEW: CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.WithOrigins("https://localhost:5001")
                      .AllowAnyMethod()
                      .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ATLAS API v1");
    });
    app.MapOpenApi(); // Or UseSwagger/UseSwaggerUI
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor"); // 🚨 NEW
//app.UseAuthentication(); // 🚨 NEW
//app.UseAuthorization(); // 🚨 NEW
app.UseMiddleware<ATLAS.API.Middleware.GlobalExceptionMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }