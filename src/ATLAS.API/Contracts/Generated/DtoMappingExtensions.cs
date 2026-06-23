#nullable enable

using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;
using System;

namespace ATLAS.API.Contracts.Generated
{
    /// <summary>
    /// Mapping extensions between NSwag-generated *Response types and Application layer *Dto types.
    /// These mappings handle type conversions (e.g., DateTimeOffset? ↔ DateTime?, Uri ↔ string).
    /// </summary>
    public static class DtoMappingExtensions
    {
        #region ApplicationSummary: Response ↔ Dto
        public static ApplicationSummaryDto ToApplicationDto(this ApplicationSummaryResponse response)
        {
            return new ApplicationSummaryDto
            {
                Id = response.Id,
                ApplicationNumber = response.ApplicationNumber,
                Status =  ToDomainStatus(response.Status),
                SubmittedDate = response.SubmittedDate?.DateTime, // DateTimeOffset? → DateTime?
                CitizenId = response.CitizenId,
                PermitTypeId = response.PermitTypeId,
                CitizenName = response.CitizenName,
                PermitTypeName = response.PermitTypeName
            };
        }

        public static ApplicationSummaryResponse ToResponse(this ApplicationSummaryDto dto)
        {
            return new ApplicationSummaryResponse
            {
                Id = dto.Id,
                ApplicationNumber = dto.ApplicationNumber,
                Status = ToApiStatus( dto.Status),
                SubmittedDate = dto.SubmittedDate.HasValue ? new DateTimeOffset(dto.SubmittedDate.Value) : null,
                CitizenId = dto.CitizenId,
                PermitTypeId = dto.PermitTypeId,
                CitizenName = dto.CitizenName,
                PermitTypeName = dto.PermitTypeName
            };
        }
        #endregion

        #region ApplicationDetail: Response ↔ Dto
        public static ApplicationDetailDto ToApplicationDto(this ApplicationDetailResponse response)
        {
            var detailDto = new ApplicationDetailDto
            {
                ReviewedDate = response.ReviewedDate?.DateTime, // DateTimeOffset? → DateTime?
                CitizenNotes = response.CitizenNotes,
                OfficerNotes = response.OfficerNotes,
                Documents = new List<DocumentDto>(),
                Reviews = new List<ReviewDto>(),
                OfficerName = response.OfficerName
            };

            // Map base properties
            detailDto.Id = response.Id;
            detailDto.ApplicationNumber = response.ApplicationNumber;
            detailDto.Status = ToDomainStatus(response.Status);
            detailDto.SubmittedDate = response.SubmittedDate?.DateTime;
            detailDto.CitizenId = response.CitizenId;
            detailDto.PermitTypeId = response.PermitTypeId;
            detailDto.CitizenName = response.CitizenName;
            detailDto.PermitTypeName = response.PermitTypeName;

            // Map collections
            if (response.Documents != null)
            {
                foreach (var doc in response.Documents)
                {
                    detailDto.Documents.Add(doc.ToApplicationDto());
                }
            }

            if (response.Reviews != null)
            {
                foreach (var review in response.Reviews)
                {
                    detailDto.Reviews.Add(review.ToApplicationDto());
                }
            }

            // Map field values
            if (response.FieldValues != null)
            {
                foreach (var fv in response.FieldValues)
                {
                    detailDto.FieldValues[fv.FieldName] = fv.Value;
                }
            }

            return detailDto;
        }

        public static ApplicationDetailResponse ToResponse(this ApplicationDetailDto dto)
        {
            var response = new ApplicationDetailResponse
            {
                ReviewedDate = dto.ReviewedDate.HasValue ? new DateTimeOffset(dto.ReviewedDate.Value) : null,
                CitizenNotes = dto.CitizenNotes,
                OfficerNotes = dto.OfficerNotes,
                Documents = new List<DocumentResponse>(),
                Reviews = new List<ReviewResponse>(),
                OfficerName = dto.OfficerName
            };

            // Map base properties
            response.Id = dto.Id;
            response.ApplicationNumber = dto.ApplicationNumber;
            response.Status = ToApiStatus(dto.Status);
            response.SubmittedDate = dto.SubmittedDate.HasValue ? new DateTimeOffset(dto.SubmittedDate.Value) : null;
            response.CitizenId = dto.CitizenId;
            response.PermitTypeId = dto.PermitTypeId;
            response.CitizenName = dto.CitizenName;
            response.PermitTypeName = dto.PermitTypeName;

            // Map collections
            foreach (var doc in dto.Documents)
            {
                response.Documents.Add(doc.ToResponse());
            }

            foreach (var review in dto.Reviews)
            {
                response.Reviews.Add(review.ToResponse());
            }

            // Map field values
            if (dto.FieldValues != null && dto.FieldValues.Count > 0)
            {
                response.FieldValues = new List<FieldValueResponse>();
                var sortOrder = 0;
                foreach (var kvp in dto.FieldValues)
                {
                    response.FieldValues.Add(new FieldValueResponse
                    {
                        FieldName = kvp.Key,
                        Value = kvp.Value,
                        SortOrder = sortOrder++
                    });
                }
            }

            return response;
        }
        #endregion

        #region Document: Response ↔ Dto
        public static DocumentDto ToApplicationDto(this DocumentResponse response)
        {
            return new DocumentDto
            {
                Id = response.Id,
                FileName = response.FileName,
                ContentType = response.ContentType,
                FileSize = response.FileSize,
                BlobUrl = response.BlobUrl?.ToString() ?? string.Empty, // Uri → string
                UploadedDate = response.UploadedDate.DateTime, // DateTimeOffset → DateTime
                UploadedById = response.UploadedById
            };
        }

        public static DocumentResponse ToResponse(this DocumentDto dto)
        {
            return new DocumentResponse
            {
                Id = dto.Id,
                FileName = dto.FileName,
                ContentType = dto.ContentType,
                FileSize = dto.FileSize,
                BlobUrl = new Uri(dto.BlobUrl), // string → Uri
                UploadedDate = new DateTimeOffset(dto.UploadedDate),
                UploadedById = dto.UploadedById
            };
        }
        #endregion

        #region Review: Response ↔ Dto
        public static ReviewDto ToApplicationDto(this ReviewResponse response)
        {
            return new ReviewDto
            {
                Id = response.Id,
                OfficerId = response.OfficerId,
                Decision = ToDomainReviewDecision( response.Decision), 
                ReasonCode = response.ReasonCode,
                Comments = response.Comments,
                ReviewedDate = response.ReviewedDate.DateTime, // DateTimeOffset → DateTime
                IsVisibleToCitizen = response.IsVisibleToCitizen
            };
        }

        public static ReviewResponse ToResponse(this ReviewDto dto)
        {
            return new ReviewResponse
            {
                Id = dto.Id,
                OfficerId = dto.OfficerId,
                Decision = ToApiReviewDecision(dto.Decision),
                ReasonCode = dto.ReasonCode,
                Comments = dto.Comments,
                ReviewedDate = new DateTimeOffset(dto.ReviewedDate),
                IsVisibleToCitizen = dto.IsVisibleToCitizen
            };
        }
        #endregion

        #region PermitType: Response ↔ Dto
        public static PermitTypeDto ToApplicationDto(this PermitTypeResponse response)
        {
            return new PermitTypeDto
            {
                Id = response.Id,
                Name = response.Name,
                Description = response.Description,
                Fee = response.Fee,
                IsActive = response.IsActive
            };
        }

        public static PermitTypeSummaryResponse ToResponse(this PermitTypeSummaryDto dto)
        {
            return new PermitTypeSummaryResponse
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Fee = dto.Fee,
                IsActive = dto.IsActive
            };

            
        }
        #endregion

        #region User: Response ↔ Dto
        public static UserDto ToApplicationDto(this UserResponse response)
        {
            return new UserDto
            {
                Id = response.Id,
                Email = response.Email,
                FirstName = response.FirstName,
                LastName = response.LastName,
                Role = response.Role                
            };
        }

        public static UserResponse ToResponse(this UserDto dto)
        {
            return new UserResponse
            {
                Id = dto.Id,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role                
            };
        }
        #endregion

        #region AuditLog: Response ↔ Dto
        public static AuditLogDto ToApplicationDto(this AuditLogResponse response)
        {
            return new AuditLogDto
            {
                Id = response.Id,
                UserId = response.UserId,
                Action = response.Action,
                EntityType = response.EntityType,
                EntityId = response.EntityId,
                Details = response.Details,
                Timestamp = response.Timestamp.DateTime, // DateTimeOffset → DateTime
                IpAddress = response.IpAddress
            };
        }

        public static AuditLogResponse ToResponse(this AuditLogDto dto)
        {
            return new AuditLogResponse
            {
                Id = dto.Id,
                UserId = dto.UserId,
                Action = dto.Action,
                EntityType = dto.EntityType,
                EntityId = dto.EntityId,
                Details = dto.Details,
                Timestamp = new DateTimeOffset(dto.Timestamp),
                IpAddress = dto.IpAddress
            };
        }
        #endregion

        #region PermitTypeSummary: Dto → Response
        public static PermitTypeResponse ToResponse(this PermitTypeDto dto)
        {
            var response = new PermitTypeResponse
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Fee = dto.Fee,
                IsActive = dto.IsActive
            };

            if (dto.Fields != null)
            {
                response.Fields = new List<FieldDefinitionResponse>();
                foreach (var field in dto.Fields)
                {
                    response.Fields.Add(field.ToResponse());
                }
            }

            return response;
        }
        #endregion

        #region FieldDefinition: Dto → Response
        public static FieldDefinitionResponse ToResponse(this FieldDefinitionDto dto)
        {            
            return new FieldDefinitionResponse
            {
                Name = dto.Name,
                Type = ToApiFieldType(dto.Type),
                IsRequired = dto.IsRequired,
                DefaultValue = dto.DefaultValue,
                Options = dto.Options.ToList()
            };
        }
        #endregion

        #region CitizenDashboard: Dto → Response
        // CitizenDashboardDto maps to ApplicationSummaryResponse
        // Properties: ApplicationId, ApplicationNumber, PermitTypeName, Status, SubmittedDate, LastUpdated
        public static ApplicationSummaryResponse ToResponse(this CitizenDashboardDto dto)
        {
            return new ApplicationSummaryResponse
            {
                Id = dto.ApplicationId, // Note: DTO uses ApplicationId, Response uses Id
                ApplicationNumber = dto.ApplicationNumber,
                Status = ToApiStatus(dto.Status), 
                SubmittedDate = dto.SubmittedDate.HasValue ? new DateTimeOffset(dto.SubmittedDate.Value) : null,
                // CitizenId, PermitTypeId, CitizenName not in DTO - leave as defaults
                PermitTypeName = dto.PermitTypeName
            };
        }
        #endregion

        private static ApplicationSummaryResponseStatus ToApiStatus(ApplicationStatus status) => status switch
        {
            ApplicationStatus.Draft => ApplicationSummaryResponseStatus.Draft,
            ApplicationStatus.Submitted => ApplicationSummaryResponseStatus.Submitted,
            ApplicationStatus.UnderReview => ApplicationSummaryResponseStatus.UnderReview,
            ApplicationStatus.Approved => ApplicationSummaryResponseStatus.Approved,
            ApplicationStatus.Rejected => ApplicationSummaryResponseStatus.Rejected,
            ApplicationStatus.InfoRequested => ApplicationSummaryResponseStatus.InfoRequested,
            ApplicationStatus.Resubmitted => ApplicationSummaryResponseStatus.Resubmitted,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        private static ApplicationStatus ToDomainStatus(ApplicationSummaryResponseStatus status) => status switch
        {
            ApplicationSummaryResponseStatus.Draft => ApplicationStatus.Draft,
            ApplicationSummaryResponseStatus.Submitted => ApplicationStatus.Submitted,
            ApplicationSummaryResponseStatus.UnderReview => ApplicationStatus.UnderReview,
            ApplicationSummaryResponseStatus.Approved => ApplicationStatus.Approved,
            ApplicationSummaryResponseStatus.Rejected => ApplicationStatus.Rejected,
            ApplicationSummaryResponseStatus.InfoRequested => ApplicationStatus.InfoRequested,
            ApplicationSummaryResponseStatus.Resubmitted => ApplicationStatus.Resubmitted,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        private static FieldDefinitionResponseType ToApiFieldType(FieldType domainType) => domainType switch
        {
            FieldType.Text => FieldDefinitionResponseType.Text,
            FieldType.MultilineText => FieldDefinitionResponseType.MultilineText,
            FieldType.Number => FieldDefinitionResponseType.Number,
            FieldType.Date => FieldDefinitionResponseType.Date,
            FieldType.Boolean => FieldDefinitionResponseType.Boolean,
            FieldType.Dropdown => FieldDefinitionResponseType.Dropdown,
            _ => throw new ArgumentOutOfRangeException(nameof(domainType))
        };

        private static FieldType ToDomainFieldType(FieldDefinitionResponseType apiType) => apiType switch
        {
            FieldDefinitionResponseType.Text => FieldType.Text,
            FieldDefinitionResponseType.MultilineText => FieldType.MultilineText,
            FieldDefinitionResponseType.Number => FieldType.Number,
            FieldDefinitionResponseType.Date => FieldType.Date,
            FieldDefinitionResponseType.Boolean => FieldType.Boolean,
            FieldDefinitionResponseType.Dropdown => FieldType.Dropdown,
            _ => throw new ArgumentOutOfRangeException(nameof(apiType))
        };

        private static ReviewResponseDecision ToApiReviewDecision(ReviewDecision domainType) => domainType switch
        {
            ReviewDecision.Approve => ReviewResponseDecision.Approve,
            ReviewDecision.Reject => ReviewResponseDecision.Reject,
            ReviewDecision.RequestInfo => ReviewResponseDecision.RequestInfo,            
            _ => throw new ArgumentOutOfRangeException(nameof(domainType))
        };

        private static ReviewDecision ToDomainReviewDecision(ReviewResponseDecision apiType) => apiType switch
        {
            ReviewResponseDecision.Approve => ReviewDecision.Approve,
            ReviewResponseDecision.Reject => ReviewDecision.Reject,
            ReviewResponseDecision.RequestInfo => ReviewDecision.RequestInfo,            
            _ => throw new ArgumentOutOfRangeException(nameof(apiType))
        };
    }    
}
