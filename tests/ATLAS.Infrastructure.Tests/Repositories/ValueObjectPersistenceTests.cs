using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.ValueObjects;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class ValueObjectPersistenceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PermitTypeRepository _repository;

        public ValueObjectPersistenceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new PermitTypeRepository(_context);
        }

        [Fact]
        public async Task PermitField_ValueObject_ShouldPersistAndRetrieve()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 150.00m);
            permitType.AddField("SiteArea", FieldType.Number, true, "0");
            permitType.AddField("ConstructionDate", FieldType.Date, false, "2026-01-01"); // Provide default value

            // Act
            await _repository.AddAsync(permitType);
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _repository.GetByIdAsync(permitType.Id);
            Assert.Equal(2, retrieved.Fields.Count);
            
            var siteAreaField = retrieved.Fields.First(f => f.Name == "SiteArea");
            Assert.Equal(FieldType.Number, siteAreaField.Type);
            Assert.True(siteAreaField.IsRequired);
            Assert.Equal("0", siteAreaField.DefaultValue);

            var dateField = retrieved.Fields.First(f => f.Name == "ConstructionDate");
            Assert.Equal(FieldType.Date, dateField.Type);
            Assert.False(dateField.IsRequired);
            Assert.Equal("2026-01-01", dateField.DefaultValue);
        }

        [Fact]
        public async Task DocumentRequirement_ValueObject_ShouldPersistAndRetrieve()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description", 150.00m);
            permitType.AddDocumentRequirement("SitePlan", true, new[] { ".pdf", ".dwg" }, 5 * 1024 * 1024);
            permitType.AddDocumentRequirement("IDDocument", true, new[] { ".pdf", ".jpg" }, 2 * 1024 * 1024);

            // Act
            await _repository.AddAsync(permitType);
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _repository.GetByIdAsync(permitType.Id);
            Assert.Equal(2, retrieved.DocumentRequirements.Count);
            
            var sitePlanReq = retrieved.DocumentRequirements.First(dr => dr.DocumentType == "SitePlan");
            Assert.True(sitePlanReq.IsRequired);
            Assert.Equal(new[] { ".pdf", ".dwg" }, sitePlanReq.AllowedExtensions);
            Assert.Equal(5 * 1024 * 1024, sitePlanReq.MaxFileSizeBytes);
        }

        [Fact]
        public async Task ApplicationStatus_Enum_ShouldPersistAndRetrieve()
        {
            // Arrange
            var application1 = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Draft app");
            var application2 = new Domain.Entities. Application(Guid.NewGuid(), Guid.NewGuid(), "Submitted app");
            application2.Submit(); // Changes status to Submitted

            _context.Applications.AddRange(application1, application2);
            await _context.SaveChangesAsync();

            // Act & Assert
            var draftApp = await _context.Applications.FirstOrDefaultAsync(a => a.Id == application1.Id);
            var submittedApp = await _context.Applications.FirstOrDefaultAsync(a => a.Id == application2.Id);

            Assert.Equal(ApplicationStatus.Draft, draftApp.Status);
            Assert.Equal(ApplicationStatus.Submitted, submittedApp.Status);
        }

        [Fact]
        public async Task ReviewDecision_Enum_ShouldPersistAndRetrieve()
        {
            // Arrange - Create separate applications for each review to avoid "already has final review" error
            var application1 = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes 1");
            var application2 = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes 2");
            
            application1.AddReview(Guid.NewGuid(), ReviewDecision.Approve, "Approved", true, null);
            application2.AddReview(Guid.NewGuid(), ReviewDecision.Reject, "Rejected", true, "Incomplete");

            _context.Applications.AddRange(application1, application2);
            await _context.SaveChangesAsync();

            // Act & Assert
            var retrieved1 = await _context.Applications.FirstOrDefaultAsync(a => a.Id == application1.Id);
            var retrieved2 = await _context.Applications.FirstOrDefaultAsync(a => a.Id == application2.Id);
            
            Assert.Contains(retrieved1.Reviews, r => r.Decision == ReviewDecision.Approve);
            Assert.Contains(retrieved2.Reviews, r => r.Decision == ReviewDecision.Reject);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
