using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using ATLAS.Application.Commands.Applications;
using Microsoft.JSInterop;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class OfficerApplicationReviewTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IJSRuntime> _jsRuntimeMock = new();
    private readonly Guid _applicationId = Guid.NewGuid();

    public OfficerApplicationReviewTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        Services.AddSingleton(_mediatorMock.Object);
        Services.AddSingleton(_currentUserMock.Object);
        Services.AddSingleton(_jsRuntimeMock.Object);
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
            Application =
            {
                ReviewedDate = DateTime.UtcNow.AddDays(-1),
                CitizenNotes = "Building a new garage",
                Documents = withDocs
                    ? new List<DocumentDto>
                    {
                        new() { Id = Guid.NewGuid(), FileName = "house-final-v7.pdf", ContentType = "application/pdf", FileSize = 4096, UploadedDate = DateTime.UtcNow },
                        new() { Id = Guid.NewGuid(), FileName = "passport.pdf", ContentType = "application/pdf", FileSize = 1024, UploadedDate = DateTime.UtcNow }
                    }
                    : new(),
                FieldValues = new Dictionary<string, string>
                {
                    { "PropertyAddress", "123 Main St" },
                    { "SquareFootage", "2000" }
                },
                Reviews = withReviews
                    ? new List<ReviewDto>
                    {
                        new() { Id = Guid.NewGuid(), OfficerId = Guid.NewGuid(), Decision = ReviewDecision.RequestInfo, ReasonCode = "INCOMPLETE", Comments = "Need survey", ReviewedDate = DateTime.UtcNow.AddDays(-1), IsVisibleToCitizen = true }
                    }
                    : new()
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
        Assert.Contains("PropertyAddress", cut.Markup);
        Assert.Contains("SquareFootage", cut.Markup);
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

    private static OfficerApplicationReviewDto SampleReviewWithAssignment(
        Guid? assignedOfficerId, bool isCurrentOfficer)
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = assignedOfficerId;
        // Simulate IsAssignedToCurrentOfficer via the view model mapping:
        // the test's CurrentUserService returns a fixed Guid; align it here.
        return dto;
    }

    [Fact]
    public void Should_ShowAssignToMe_WhenUnassigned()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = null;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default)).ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters => parameters.Add(p => p.ApplicationId, _applicationId));
        Assert.Contains("Assign to me", cut.Markup);
    }

    [Fact]
    public void Should_ShowAssignedToYou_WhenCurrentOfficer()
    {
        // The test's CurrentUserService.UserId is a fixed Guid; reuse it as the assigned officer.
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default)).ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters => parameters.Add(p => p.ApplicationId, _applicationId));
        Assert.Contains("Assigned to you", cut.Markup);
        Assert.DoesNotContain("Assign to me", cut.Markup);
    }

    [Fact]
    public void Should_NotShowAssignToMe_WhenAssignedToOtherOfficer()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = Guid.NewGuid(); // different from CurrentUserService.UserId
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default)).ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters => parameters.Add(p => p.ApplicationId, _applicationId));
        Assert.DoesNotContain("Assign to me", cut.Markup);
    }

    [Fact] 
    public void Should_ShowDecisionPanel_WhenAssignedToCurrentOfficerAndUnderReview()
    { 
        var dto = SampleReview(); dto.AssignedOfficerId = _currentUserMock.Object.UserId; dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default)).ReturnsAsync(dto);
        var cut = Render<OfficerApplicationReview>(p => p.Add(x => x.ApplicationId, _applicationId));
        Assert.Contains("Officer Decision", cut.Markup);
        Assert.Contains("Approve", cut.Markup); 
    }
    
    [Fact]
    public void Should_NotShowDecisionPanel_WhenNotAssigned()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = null;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Officer Decision", cut.Markup);
    }

    [Fact]
    public void Should_NotShowDecisionPanel_WhenAssignedToOtherOfficer()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = Guid.NewGuid(); // different from CurrentUserService.UserId
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Officer Decision", cut.Markup);
    }

    [Fact]
    public void Should_NotShowDecisionPanel_WhenStatusNotUnderReview()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.Submitted; // not UnderReview
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Officer Decision", cut.Markup);
    }

    [Fact]
    public void Should_RenderEnabledDecisionButtons_WhenCanDecide()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Approve button is present and not disabled before a decision is in flight
        var approveBtn = cut.Find("button.btn-approve, button");
        Assert.NotNull(approveBtn);
        Assert.False(approveBtn.HasAttribute("disabled"));
        Assert.Contains("Approve", cut.Markup);
    }

    [Fact]
    public async Task Should_Approve_ShowInlineConfirmation_ThenConfirm_SendsCommand()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Clicking Approve enters the inline confirmation state (no command yet)
        cut.Find("button.btn-success").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<ApproveApplicationCommand>(), default), Times.Never);
        Assert.Contains("Approve application", cut.Markup);

        // Fields and original action buttons are hidden while confirming
        Assert.DoesNotContain("decision-comments", cut.Markup);
        Assert.DoesNotContain("decision-reason-code", cut.Markup);
        Assert.DoesNotContain("Request Information", cut.Markup);

        // Confirm executes the approval
        cut.Find("[data-testid='confirm-decision']").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<ApproveApplicationCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Should_Approve_Cancel_RestoresForm_AndDoesNotSendCommand()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        cut.Find("#decision-comments").Change("Looks good");
        cut.Find("button.btn-success").Click();
        Assert.Contains("Approve application", cut.Markup);

        cut.Find("[data-testid='cancel-decision']").Click();

        _mediatorMock.Verify(m => m.Send(It.IsAny<ApproveApplicationCommand>(), default), Times.Never);
        // Confirmation hidden and form restored with preserved values
        Assert.DoesNotContain("Approve application", cut.Markup);
        Assert.Contains("decision-comments", cut.Markup);
        Assert.Contains("Looks good", cut.Markup);
    }

    [Fact]
    public async Task Should_Reject_ShowInlineConfirmation_ThenConfirm_SendsCommand()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Provide required comments and reason code
        cut.Find("#decision-comments").Change("Missing documentation");
        cut.Find("#decision-reason-code").Change("INCOMPLETE");

        cut.Find("button.btn-danger").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<RejectApplicationCommand>(), default), Times.Never);
        Assert.Contains("Reject application", cut.Markup);

        // Fields and original action buttons hidden while confirming
        Assert.DoesNotContain("decision-comments", cut.Markup);
        Assert.DoesNotContain("decision-reason-code", cut.Markup);

        cut.Find("[data-testid='confirm-decision']").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<RejectApplicationCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Should_Reject_WithoutComments_ShowsValidationError_AndNoConfirmation()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Reason code provided but comments missing
        cut.Find("#decision-reason-code").Change("INCOMPLETE");

        cut.Find("button.btn-danger").Click();

        _mediatorMock.Verify(m => m.Send(It.IsAny<RejectApplicationCommand>(), default), Times.Never);
        Assert.Contains("Comments / Instructions are required", cut.Markup);
        Assert.DoesNotContain("Reject application", cut.Markup);
        // Form remains visible (no confirmation state)
        Assert.Contains("decision-comments", cut.Markup);
    }

    [Fact]
    public async Task Should_Reject_WithoutReasonCode_ShowsValidationError_AndNoConfirmation()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // Comments provided but reason code missing
        cut.Find("#decision-comments").Change("Missing documentation");

        cut.Find("button.btn-danger").Click();

        _mediatorMock.Verify(m => m.Send(It.IsAny<RejectApplicationCommand>(), default), Times.Never);
        Assert.Contains("Rejection Reason Code is required", cut.Markup);
        Assert.DoesNotContain("Reject application", cut.Markup);
        Assert.Contains("decision-comments", cut.Markup);
    }

    [Fact]
    public async Task Should_RequestInfo_ShowInlineConfirmation_ThenConfirm_SendsCommand()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        cut.Find("#decision-comments").Change("Please provide more details");

        cut.Find("button.btn-warning").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<RequestInfoCommand>(), default), Times.Never);
        Assert.Contains("Request additional information", cut.Markup);

        // Fields and original action buttons hidden while confirming
        Assert.DoesNotContain("decision-comments", cut.Markup);
        Assert.DoesNotContain("decision-reason-code", cut.Markup);

        cut.Find("[data-testid='confirm-decision']").Click();
        _mediatorMock.Verify(m => m.Send(It.IsAny<RequestInfoCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task Should_RequestInfo_WithoutComments_ShowsValidationError_AndNoConfirmation()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        cut.Find("button.btn-warning").Click();

        _mediatorMock.Verify(m => m.Send(It.IsAny<RequestInfoCommand>(), default), Times.Never);
        Assert.Contains("Comments / Instructions are required", cut.Markup);
        Assert.DoesNotContain("Request additional information", cut.Markup);
        Assert.Contains("decision-comments", cut.Markup);
    }

    [Fact]
    public async Task Should_RefreshReview_AndHideDecisionPanel_AfterApprove()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;

        // Initial load returns UnderReview (panel visible); post-decision reload returns Approved (panel hidden)
        var approvedDto = SampleReview();
        approvedDto.AssignedOfficerId = _currentUserMock.Object.UserId;
        approvedDto.Status = ApplicationStatus.Approved;
        _mediatorMock.SetupSequence(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto)
            .ReturnsAsync(approvedDto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        cut.Find("button.btn-success").Click();
        cut.Find("[data-testid='confirm-decision']").Click();

        // Reload was triggered after the decision
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default), Times.AtLeast(2));
        // Decision panel no longer shown once status is Approved
        Assert.DoesNotContain("Officer Decision", cut.Markup);
    }

    [Fact]
    public async Task Should_ShowError_WhenDecisionFails()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);
        // Decision send throws (e.g. not assigned to this officer server-side)
        _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveApplicationCommand>(), default))
            .ThrowsAsync(new InvalidOperationException("not assigned to you"));

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        cut.Find("button.btn-success").Click();
        cut.Find("[data-testid='confirm-decision']").Click();

        // Error state surfaced to the officer
        var alert = cut.Find(".alert-danger");
        Assert.Contains("unable to record the decision", alert.TextContent);
    }

    [Fact]
    public void Should_ShowReleaseAssignment_WhenAssignedToCurrentOfficerAndUnderReview()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.Contains("Release Assignment", cut.Markup);
        Assert.Contains("Assigned to you", cut.Markup);
    }

    [Fact]
    public void Should_NotShowReleaseAssignment_WhenUnassigned()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = null;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Release Assignment", cut.Markup);
    }

    [Fact]
    public void Should_NotShowReleaseAssignment_WhenAssignedToOtherOfficer()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = Guid.NewGuid(); // different from current user
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Release Assignment", cut.Markup);
    }

    [Fact]
    public void Should_NotShowReleaseAssignment_WhenNotUnderReview()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.Submitted; // not UnderReview
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        Assert.DoesNotContain("Release Assignment", cut.Markup);
    }

    [Fact]
    public async Task Should_ReleaseAssignment_Immediately_WhenClicked()
    {
        var dto = SampleReview();
        dto.AssignedOfficerId = _currentUserMock.Object.UserId;
        dto.Status = ApplicationStatus.UnderReview;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerApplicationReviewQuery>(), default))
            .ReturnsAsync(dto);

        var cut = Render<OfficerApplicationReview>(parameters =>
            parameters.Add(p => p.ApplicationId, _applicationId));

        // No confirmation dialog — clicking Release Assignment executes immediately.
        cut.Find("button.btn-secondary").Click();

        _mediatorMock.Verify(m => m.Send(It.IsAny<ReleaseApplicationCommand>(), default), Times.Once);
        _jsRuntimeMock.Verify(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>()), Times.Never);
    }
}