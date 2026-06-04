using System;
using System.Collections.Generic;

namespace ATLAS.Application.DTOs
{
    public class ApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public string ApplicationNumber { get; set; }
        public int Status { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public Guid CitizenId { get; set; }
        public Guid PermitTypeId { get; set; }
    }

    public class ApplicationDetailDto : ApplicationSummaryDto
    {
        public DateTime? ReviewedDate { get; set; }
        public string CitizenNotes { get; set; } = string.Empty;
        public string OfficerNotes { get; set; } = string.Empty;
        public List<DocumentDto> Documents { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid OfficerId { get; set; }
        public int Decision { get; set; }
        public string? ReasonCode { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime ReviewedDate { get; set; }
        public bool IsVisibleToCitizen { get; set; }
    }
}
