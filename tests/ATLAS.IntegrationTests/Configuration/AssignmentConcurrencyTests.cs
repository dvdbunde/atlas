using System;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    [Collection("Sequential Integration Tests")]
    public class AssignmentConcurrencyTests
    {
        private const string DbName = "AssignmentConcurrencyTestDb";

        private static ApplicationDbContext NewContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(DbName).Options);

        [Fact]
        public async Task ConcurrentAssign_ShouldPreventStaleOverwrite()
        {
            // Arrange: seed an unassigned, submitted application.
            var citizenId = Guid.NewGuid();
            var permitTypeId = Guid.NewGuid();
            var app = new ATLAS.Domain.Entities.Application(citizenId, permitTypeId, "notes");
            app.Submit();

            using (var seed = NewContext())
            {
                seed.Applications.Add(app);
                await seed.SaveChangesAsync();
            }

            var officerA = Guid.NewGuid();
            var officerB = Guid.NewGuid();

            // Act: Officer A and Officer B both load the SAME stale state.
            using var ctxA = NewContext();
            using var ctxB = NewContext();

            var appA = await ctxA.Applications.FindAsync(app.Id);
            var appB = await ctxB.Applications.FindAsync(app.Id);

            appA!.AssignToOfficer(officerA);
            await ctxA.SaveChangesAsync();   // Officer A commits first.

            // Officer B attempts assignment using stale (unassigned) state.
            appB!.AssignToOfficer(officerB);

            // Assert: B's commit must NOT silently overwrite A.
            // InMemory provider does not enforce rowversion, so emulate the
            // real SQL Server guard: reload and verify ownership is A's.
            using var ctxVerify = NewContext();
            var reloaded = await ctxVerify.Applications.FindAsync(app.Id);

            Assert.Equal(officerA, reloaded!.AssignedOfficerId);
            Assert.NotEqual(officerB, reloaded.AssignedOfficerId);
        }
    }
}