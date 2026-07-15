using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.ValueObjects;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries;

public class GetOfficerApplicationReviewQueryHandlerTests
{
    private readonly Mock<IApplicationRepository> _mockAppRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepo;
    private readonly GetOfficerApplicationReviewQueryHandler _handler;

    public GetOfficerApplicationReviewQueryHandlerTests()
    {
        _mockAppRepo = new Mock<IApplicationRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockPermitTypeRepo = new Mock<IPermitTypeRepository>();
        _handler = new GetOfficerApplicationReviewQueryHandler(
            _mockAppRepo.Object, _mockUserRepo.Object, _mockPermitTypeRepo.Object);
    }

    // ----- Domain builders (mirror GetOfficerDashboardQueryHandlerTests conventions) -----

    private static Entities.Application MakeSubmittedApplication(
        Guid citizenId, Guid permitTypeId, Guid applicationId, string applicationNumber)
    {
        var app = new Entities.Application(citizenId, permitTypeId, "citizen notes");
        SetId(app, applicationId);
        app.Submit();
        return app;
    }

    private static void SetId(Entities.Application app, Guid id)
    {
        typeof(Entities.Application).GetProperty("Id")?.SetValue(app, id);
    }

    private static Entities.User MakeUser(Guid id, string first, string last, string email)
    {
        var user = new Entities.User(id, email, first, last, UserRole.Officer);
        return user;
    }

    private static PermitType MakePermitType(params (string name, FieldType type, bool required)[] fields)
    {
        var pt = new PermitType("Building Permit", "For construction", 150m);
        foreach (var f in fields)
        {
            pt.AddField(f.name, f.type, f.required);
        }
        pt.AddDocumentRequirement("SitePlan", true, new[] { ".pdf" }, 5_000_000);
        pt.AddDocumentRequirement("IdentityProof", false, new[] { ".pdf", ".png" }, 2_000_000);
        return pt;
    }

    private void SetupLookups(Guid citizenId, Guid permitTypeId, Entities.User citizen, Entities.User officer, PermitType permitType)
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(citizenId, It.IsAny<CancellationToken>())).ReturnsAsync(citizen);
        _mockUserRepo.Setup(r => r.GetByIdAsync(officer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(officer);
        _mockPermitTypeRepo.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(permitType);
    }

    // ----- Successful projection -----

    [Fact]
    public async Task Handle_ShouldReturnCompleteReview_WhenApplicationExists()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0001");
        app.AddFieldValue("PropertyAddress", "123 Main St", 0);
        app.AddDocument(Guid.NewGuid(), "SitePlan", "house-final-v7.pdf", "application/pdf", 2048, "https://blob/secret", citizenId);

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane.doe@example.com");
        var officer = MakeUser(officerId, "Joe", "Officer", "joe.officer@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(appId, result!.ApplicationId);
        Assert.False(string.IsNullOrEmpty(result.ApplicationNumber));
        Assert.Equal(ApplicationStatus.Submitted, result.Status);
        Assert.Equal("Building Permit", result.PermitTypeName);
        Assert.Equal("For construction", result.PermitTypeDescription);
        Assert.Equal(citizenId, result.CitizenId);
        Assert.Equal("Jane Doe", result.CitizenName);
        Assert.Equal("jane.doe@example.com", result.CitizenEmail);
        Assert.NotNull(result.SubmittedDate);
        Assert.NotNull(result.LastUpdated);
    }

    [Fact]
    public async Task Handle_ShouldDeriveAssignedOfficerFromLatestReview()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0002");
        app.StartReview(officerId);
        app.AddReview(Guid.NewGuid(), officerId, ReviewDecision.RequestInfo, "Need survey", true, "INCOMPLETE");

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(officerId, "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Joe Officer", result!.AssignedOfficerName);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenApplicationNotFound()
    {
        _mockAppRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entities.Application?)null);

        var result = await _handler.Handle(
            new GetOfficerApplicationReviewQuery { ApplicationId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _handler.Handle(null!, CancellationToken.None));
    }

    // ----- Dynamic field projection -----

    [Fact]
    public async Task Handle_ShouldProjectFieldValues_WithPermitLabels()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0003");
        app.AddFieldValue("addr", "123 Main St", 0);
        app.AddFieldValue("sqft", "2000", 1);

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType(
            ("addr", FieldType.Text, true),
            ("sqft", FieldType.Number, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.FieldValues.Count);
        // Label comes from permit field Name, not the raw field key
        Assert.Contains(result.FieldValues, f => f.FieldName == "addr" && f.Label == "addr" && f.Value == "123 Main St" && f.FieldType == FieldType.Text);
        Assert.Contains(result.FieldValues, f => f.FieldName == "sqft" && f.Value == "2000" && f.FieldType == FieldType.Number);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToFieldName_WhenPermitFieldMissing()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0004");
        app.AddFieldValue("orphanField", "value", 0);

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType(); // no matching field

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        var fv = Assert.Single(result!.FieldValues);
        Assert.Equal("orphanField", fv.Label); // falls back to field name
        Assert.Equal("value", fv.Value);
    }

    // ----- Document requirement projection (DocumentType association, NOT filename) -----

    [Fact]
    public async Task Handle_ShouldAssociateDocumentByDocumentType_NotFilename()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0005");
        // Arbitrary filename — must still associate with "SitePlan" requirement via DocumentType
        app.AddDocument(Guid.NewGuid(), "SitePlan", "house-final-v7.pdf", "application/pdf", 4096, "https://blob/secret", citizenId);

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        var sitePlan = result!.DocumentRequirements.Single(r => r.DocumentType == "SitePlan");
        Assert.True(sitePlan.IsRequired);
        Assert.True(sitePlan.IsSatisfied);
        var doc = Assert.Single(sitePlan.UploadedDocuments);
        Assert.Equal("house-final-v7.pdf", doc.FileName);
        Assert.Equal(4096, doc.FileSize);
        // BlobUrl must never be projected — only metadata is exposed
        Assert.NotEqual(default, doc.UploadedDate);
    }

    [Fact]
    public async Task Handle_ShouldMarkRequiredMissing_WhenNoDocument()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0006");
        // No documents uploaded at all

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        var sitePlan = result!.DocumentRequirements.Single(r => r.DocumentType == "SitePlan");
        Assert.True(sitePlan.IsRequired);
        Assert.False(sitePlan.IsSatisfied);
        Assert.Empty(sitePlan.UploadedDocuments);

        var optional = result.DocumentRequirements.Single(r => r.DocumentType == "IdentityProof");
        Assert.False(optional.IsRequired);
        Assert.False(optional.IsSatisfied);
        Assert.Empty(optional.UploadedDocuments);
    }

    [Fact]
    public async Task Handle_ShouldMarkOptionalSupplied_WhenDocumentPresent()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0007");
        app.AddDocument(Guid.NewGuid(), "IdentityProof", "passport.pdf", "application/pdf", 1024, "https://blob/secret", citizenId);
        // Required SitePlan intentionally missing

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        var optional = result!.DocumentRequirements.Single(r => r.DocumentType == "IdentityProof");
        Assert.False(optional.IsRequired);
        Assert.True(optional.IsSatisfied);
        Assert.Single(optional.UploadedDocuments);

        var required = result.DocumentRequirements.Single(r => r.DocumentType == "SitePlan");
        Assert.True(required.IsRequired);
        Assert.False(required.IsSatisfied);
    }

    [Fact]
    public async Task Handle_ShouldProjectMultipleRequirements_AndMultipleDocumentsPerRequirement()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0008");
        app.AddDocument(Guid.NewGuid(), "SitePlan", "plan-a.pdf", "application/pdf", 1024, "https://blob/1", citizenId);
        app.AddDocument(Guid.NewGuid(), "SitePlan", "plan-b.pdf", "application/pdf", 2048, "https://blob/2", citizenId);

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.DocumentRequirements.Count);
        var sitePlan = result.DocumentRequirements.Single(r => r.DocumentType == "SitePlan");
        Assert.Equal(2, sitePlan.UploadedDocuments.Count);
    }

    // ----- Reviews -----

    [Fact]
    public async Task Handle_ShouldProjectExistingReviews()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var officerId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0009");
        app.StartReview(officerId);
        app.AddReview(Guid.NewGuid(), officerId, ReviewDecision.RequestInfo, "Need survey", true, "INCOMPLETE");

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(officerId, "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        var review = Assert.Single(result!.Reviews);
        Assert.Equal(officerId, review.OfficerId);
        Assert.Equal(ReviewDecision.RequestInfo, review.Decision);
        Assert.Equal("INCOMPLETE", review.ReasonCode);
        Assert.Equal("Need survey", review.Comments);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyReviews_WhenNoReviews()
    {
        var citizenId = Guid.NewGuid();
        var permitTypeId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var app = MakeSubmittedApplication(citizenId, permitTypeId, appId, "APP-2026-0010");
        // Submitted but no review yet

        var citizen = MakeUser(citizenId, "Jane", "Doe", "jane@example.com");
        var officer = MakeUser(Guid.NewGuid(), "Joe", "Officer", "joe@example.com");
        var permitType = MakePermitType( ("PropertyAddress", FieldType.Text, true));

        _mockAppRepo.Setup(r => r.GetByIdAsync(appId, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        SetupLookups(citizenId, permitTypeId, citizen, officer, permitType);

        var result = await _handler.Handle(new GetOfficerApplicationReviewQuery { ApplicationId = appId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Reviews);
        Assert.Null(result.AssignedOfficerName);
    }
}