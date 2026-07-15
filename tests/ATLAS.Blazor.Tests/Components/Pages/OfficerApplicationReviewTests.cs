using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class OfficerApplicationReviewTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public OfficerApplicationReviewTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static OfficerApplicationReviewDto SampleReview(bool withDocs = true, bool withReviews = true)
    {
        var requirements = new List<OfficerDocumentRequirementDto>
        {
            new()
            {
                DocumentType = "SitePlan",
                IsRequired = true,
                IsSatisfied = withDocs,
                UploadedDocuments = withDocs
                    ? new List<OfficerDocumentDto>
                    {
                        new() { Id = Guid.NewGuid(), FileName = "house-final-v7.pdf", ContentType = "application/pdf", FileSize = 4096, UploadedDate = DateTime.UtcNow }
                    }
                    : new()
            },
            new()
            {
                DocumentType = "IdentityProof",
                IsRequired = false,
                IsSatisfied = withDocs,
                UploadedDocuments = withDocs
                    ? new List<OfficerDocumentDto>
                    {
                        new() { Id = Guid.NewGuid(), FileName = "passport.pdf", ContentType = "application/pdf", FileSize = 1024, UploadedDate = DateTime.UtcNow }
                    }
                    : new()
            }
        };

        return new OfficerApplicationReviewDto
        {
            ApplicationId = Guid.NewGuid(),
            ApplicationNumber = "APP-2026-0042",
            Status = ApplicationStatus.Submitted,
            PermitTypeName = "Building Permit",
            PermitTypeDescription = "For construction",
            SubmittedDate = DateTime.UtcNow.AddDays(-2),
            LastUpdated = DateTime.UtcNow.AddDays(-1),
            CitizenId = Guid.NewGuid(),
            CitizenName = "Jane Doe",
            CitizenEmail = "jane.doe@example.com",
            AssignedOfficerName = "Joe Officer",
            CitizenNotes = "Building a new garage",
            FieldValues = new List<OfficerFieldValueDto>
            {
                new() { FieldName = "PropertyAddress", Label = "Property Address", Value = "123 Main St", FieldType = FieldType.Text },
                new() { FieldName = "SquareFootage", Label = "Square Footage", Value = "2000", FieldType = FieldType.Number }
            },
            DocumentRequirements = requirements,
            Reviews = withReviews
                ? new List<OfficerReviewDto>
                {
                    new() { Id = Guid.NewGuid(), OfficerId = Guid.NewGuid(), Decision = ReviewDecision.RequestInfo, ReasonCode = "INCOMPLETE", Comments = "Need survey", ReviewedDate = DateTime.UtcNow.AddDays(-1) }
                }
                : new()
        };
    }

    // ----- Page loading -----

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<OfficerApplicationReviewDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default)).Returns(tcs.Task);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderOverview_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview());

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("APP-2026-0042", cut.Markup);
        Assert.Contains("Building Permit", cut.Markup);
        Assert.Contains("Jane Doe", cut.Markup);
        Assert.Contains("jane.doe@example.com", cut.Markup);
        Assert.Contains("Joe Officer", cut.Markup);
        Assert.Contains("Building a new garage", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync((OfficerApplicationReviewDto?)null);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        var alert = cut.Find(".alert-danger");
        Assert.Contains("not found", alert.TextContent);
    }

    // ----- Dynamic fields (read-only) -----

    [Fact]
    public void Should_ShowSubmittedFieldValues_ReadOnly()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview());

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Meaningful permit labels are rendered
        Assert.Contains("Property Address", cut.Markup);
        Assert.Contains("Square Footage", cut.Markup);
        // Submitted values are rendered
        Assert.Contains("123 Main St", cut.Markup);
        Assert.Contains("2000", cut.Markup);
        // No editable inputs — read-only plaintext only
        Assert.Empty(cut.FindAll("input"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    // ----- Document requirements -----

    [Fact]
    public void Should_ShowRequiredUploaded_AsSatisfied()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview(withDocs: true));

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // SitePlan requirement shows Uploaded + the arbitrary filename
        Assert.Contains("SitePlan", cut.Markup);
        Assert.Contains("Uploaded", cut.Markup);
        Assert.Contains("house-final-v7.pdf", cut.Markup);
        // Download link present, no delete/replace controls
        Assert.Contains("/documents/", cut.Markup);
        Assert.DoesNotContain("Delete", cut.Markup);
    }

    [Fact]
    public void Should_ShowRequiredMissing_AsMissing()
    {
        // Build a review where SitePlan is required but not satisfied
        var review = SampleReview(withDocs: false);
        review.DocumentRequirements.Single(r => r.DocumentType == "SitePlan").IsSatisfied = false;
        review.DocumentRequirements.Single(r => r.DocumentType == "SitePlan").UploadedDocuments = new();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(review);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("SitePlan", cut.Markup);
        Assert.Contains("Missing", cut.Markup);
        Assert.Contains("No document uploaded", cut.Markup);
    }

    [Fact]
    public void Should_ShowOptionalSupplied_Correctly()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview(withDocs: true));

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("IdentityProof", cut.Markup);
        Assert.Contains("Optional", cut.Markup);
        Assert.Contains("passport.pdf", cut.Markup);
    }

    [Fact]
    public void Should_ShowOptionalUnsupplied_Correctly()
    {
        var review = SampleReview(withDocs: false);
        review.DocumentRequirements.Single(r => r.DocumentType == "IdentityProof").IsSatisfied = false;
        review.DocumentRequirements.Single(r => r.DocumentType == "IdentityProof").UploadedDocuments = new();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(review);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("IdentityProof", cut.Markup);
        Assert.Contains("Optional", cut.Markup);
        Assert.Contains("Not supplied", cut.Markup);
    }

    // ----- Reviews -----

    [Fact]
    public void Should_ShowExistingReviews()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview(withReviews: true));

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("Need survey", cut.Markup);
        Assert.Contains("INCOMPLETE", cut.Markup);
    }

    [Fact]
    public void Should_ShowEmptyReviewState()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview(withReviews: false));

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("No reviews recorded", cut.Markup);
    }

    // ----- Navigation -----

    [Fact]
    public void Should_ProvideBackNavigation_ToOfficerDashboard()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(SampleReview());

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        var backLink = cut.Find("a[href='/officer/dashboard']");
        Assert.NotNull(backLink);
        Assert.Contains("Officer Dashboard", backLink.TextContent);
    }
}