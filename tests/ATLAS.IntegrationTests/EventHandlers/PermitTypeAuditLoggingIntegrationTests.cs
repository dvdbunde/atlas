using System.Security.Claims;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.IntegrationTests;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ATLAS.integrationTests.EventHandlers;

[Collection("Sequential Integration Tests")]
public class PermitTypeAuditLoggingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PermitTypeAuditLoggingIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        // Direct MediatR sends run outside an HTTP request, so the ambient
        // CurrentUserService (HttpContext-based) has no user. Override it for this
        // test class only with a fixed admin identity, mirroring TestUserBuilder.AsAdmin().
        _factory = factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserService>();
                services.AddScoped<ICurrentUserService, FixedAdminCurrentUserService>();
            }));
    }

    [Fact]
    public async Task AddPermitField_ShouldCreateAuditLog_WithActingAdminUserId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var permitType = new PermitType("Audit Test Permit", "Desc", 50m);
        db.PermitTypes.Add(permitType);
        await db.SaveChangesAsync();

        var adminId = TestData.AdminUserId;

        // Act
        await mediator.Send(new AddPermitFieldCommand
        {
            PermitTypeId = permitType.Id,
            Name = "InspectionNotes",
            Type = ATLAS.Domain.Enums.FieldType.Text,
            IsRequired = true
        }, default);

        // Assert
        var field = (await db.Entry(permitType).Collection(p => p.Fields)
            .Query().AsNoTracking().ToListAsync()).Last();
        var auditLogs = await db.AuditLogs
            .Where(a => a.EntityType == "PermitField" && a.EntityId == field.Id)
            .ToListAsync();

        var log = Assert.Single(auditLogs);
        Assert.Equal("Added", log.Action);
        Assert.Equal(adminId, log.UserId);
    }

    [Fact]
    public async Task AddDocumentRequirement_ShouldCreateAuditLog_WithActingAdminUserId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var permitType = new PermitType("Audit Test Permit 2", "Desc", 50m);
        db.PermitTypes.Add(permitType);
        await db.SaveChangesAsync();

        var adminId = TestData.AdminUserId;

        // Act
        await mediator.Send(new AddDocumentRequirementCommand
        {
            PermitTypeId = permitType.Id,
            DocumentType = "ProofOfAddress",
            IsRequired = true,
            AllowedExtensions = new[] { ".pdf" },
            MaxFileSizeBytes = 5_000_000
        }, default);

        // Assert
        var requirement = db.Entry(permitType).Collection(p => p.DocumentRequirements)
            .Query().AsNoTracking().First(r => r.DocumentType == "ProofOfAddress");
        var auditLogs = await db.AuditLogs
            .Where(a => a.EntityType == "DocumentRequirement" && a.EntityId == requirement.Id)
            .ToListAsync();

        var log = Assert.Single(auditLogs);
        Assert.Equal("Added", log.Action);
        Assert.Equal(adminId, log.UserId);
    }

    /// <summary>
    /// Test-only ICurrentUserService that reports a fixed admin identity, so direct
    /// MediatR sends (no HttpContext) still record the acting administrator's UserId.
    /// </summary>
    private sealed class FixedAdminCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => TestData.AdminUserId;
        public string? Email => "admin@atlas.test";
        public string? Role => "Admin";
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<Claim> Claims => Array.Empty<Claim>();
    }
}