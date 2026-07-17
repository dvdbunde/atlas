//----------------------
// Applications Controller Adapter
// Implements IApplicationsController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Commands;
using ATLAS.Application.Queries;
using ATLAS.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Queries.Applications;
using ATLAS.Domain.Enums;
using ATLAS.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ATLAS.API.Controllers
{
    [ApiController]    
    [Produces("application/json")]
    public sealed class ApplicationsController : ApplicationsControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public ApplicationsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));         
        }

        public override async Task<ActionResult<ICollection<ApplicationSummaryResponse>>> GetApplications(
            Guid? citizenId = null,
            Guid? officerId = null,
            string? status = null,
            Guid? permitTypeId = null,
            DateTimeOffset? dateFrom = null,
            DateTimeOffset? dateTo = null,
            string? search = null)            
        {
            var query = new GetApplicationsQuery
            {
                CitizenId = citizenId,
                OfficerId = officerId,
                Status = status,
                PermitTypeId = permitTypeId,
                DateFrom = dateFrom?.DateTime,
                DateTo = dateTo?.DateTime,
                Search = search
            };

            var results = await _mediator.Send(query, default);
            var response = new List<ApplicationSummaryResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            return response;
        }

        public override async Task<ActionResult<ApplicationDetailResponse>> GetApplicationById(
            Guid id)
        {
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var result = await _mediator.Send(query, default);

            if (result == null)
            {
                return NotFound();
            }

            return result.ToResponse();
        }

               public override async Task<ActionResult<bool>> ApproveApplication(
            Guid id, ApproveApplicationRequest body)
        {
            try
            {
                var result = await _mediator.Send(new ApproveApplicationCommand
                {
                    ApplicationId = id,
                    Comments = body.Comments
                }, default);

                if (!result)
                    return NotFound();
                return Ok(true);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (DomainException ex) when (ex.Message.Contains("assigned"))
            {
                return Conflict(new { error = ex.Message });
            }
        }

        public override async Task<ActionResult<bool>> RejectApplication(
            Guid id, RejectApplicationRequest body)
        {
            try
            {
                var result = await _mediator.Send(new RejectApplicationCommand
                {
                    ApplicationId = id,
                    ReasonCode = body.ReasonCode,
                    Comments = body.Comments
                }, default);

                if (!result)
                    return NotFound();
                return Ok(true);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (DomainException ex) when (ex.Message.Contains("assigned"))
            {
                return Conflict(new { error = ex.Message });
            }
        }

        public override async Task<ActionResult<bool>> RequestInfo(
            Guid id, RequestInfoRequest body)
        {
            try
            {
                var result = await _mediator.Send(new RequestInfoCommand
                {
                    ApplicationId = id,
                    Message = body.Message
                }, default);

                if (!result)
                    return NotFound();
                return Ok(true);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (DomainException ex) when (ex.Message.Contains("assigned"))
            {
                return Conflict(new { error = ex.Message });
            }
        }
        
        public override async Task<ActionResult<bool>> AssignApplicationToMe(
            Guid id, [FromBody] AssignApplicationToMeRequest body)
        {
            try
            {
                var result = await _mediator.Send(new AssignApplicationToMeCommand { ApplicationId = id }, default);
                if (!result)
                    return NotFound();
                return Ok(true);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (DomainException ex) when (ex.Message.Contains("already assigned"))
            {
                return Conflict(new { error = ex.Message });
            }
        }

        public override async Task<ActionResult<ApplicationSummaryResponse>> UpdateDraft(Guid id, [FromBody] UpdateDraftRequest body)
        {
            var command = new UpdateDraftCommand
            {
                ApplicationId = id,
                CitizenNotes = body.CitizenNotes,
                FieldValues = body.FieldValues?.ToDictionary(
                    fv => fv.FieldName,
                    fv => fv.Value) ?? new Dictionary<string, string>()
            };

            var result = await _mediator.Send(command, default);
            
            // Fetch updated application
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var application = await _mediator.Send(query, default);
            
            if (application == null)
            {
                return NotFound();
            }

            return Ok(application.ToResponse());
        }

        public override async Task<ActionResult<ApplicationSummaryResponse>> CreateDraft([FromBody] CreateDraftRequest body)
        {
            var command = new Application.Commands.Applications.CreateDraftCommand
            {
                PermitTypeId = body.PermitTypeId,
                CitizenNotes = body.CitizenNotes,
                FieldValues = body.FieldValues?.ToDictionary(
                    fv => fv.FieldName,
                    fv => fv.Value) ?? new Dictionary<string, string>()
            };

            var applicationId = await _mediator.Send(command, default);
            
            // Get the created draft to return
            var query = new GetApplicationByIdQuery { ApplicationId = applicationId };
            var result = await _mediator.Send(query, default);
            
            if (result == null)
            {
                return NotFound();
            }

            return CreatedAtAction(
                nameof(GetApplications),
                new { id = applicationId },
                result.ToResponse());
        }

        public override async Task<ActionResult<ApplicationSummaryResponse>> SubmitDraft(Guid id)
        {
            var command = new SubmitDraftCommand
            {
                ApplicationId = id
            };

            await _mediator.Send(command, default);
            
            // Fetch submitted application
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var result = await _mediator.Send(query, default);
            
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result.ToResponse());
        }

        public override async Task<ActionResult<ApplicationSummaryResponse>> ResubmitDraft(Guid id)
        {
            var command = new ResubmitApplicationCommand
            {
                ApplicationId = id
            };

            await _mediator.Send(command, default);
            
            // Fetch resubmitted application
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var result = await _mediator.Send(query, default);
            
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result.ToResponse());
        }
        
        public override async Task<ActionResult<ICollection<ApplicationSummaryResponse>>> GetCitizenDashboard()
        {
            var query = new GetCitizenDashboardQuery();
            var results = await _mediator.Send(query, default);
            
            var response = new List<ApplicationSummaryResponse>();
            foreach (var dto in results)
            {
                response.Add(dto.ToResponse());
            }
            
            return Ok(response);
        }
                
        public override async Task<ActionResult<Contracts.Generated.OfficerDashboardResult>> GetOfficerDashboard(
            [FromQuery] SortBy? sortBy = null,
            [FromQuery] bool? sortDescending = null,
            [FromQuery] int? pageNumber = null, 
            [FromQuery] int? pageSize = null, 
            [FromQuery] string? statuses = null, 
            [FromQuery] Guid? permitTypeId = null)
        {
             var query = new GetOfficerDashboardQuery
            {
                Statuses = ParseStatuses(statuses),
                PermitTypeId = permitTypeId,                
                SortBy = sortBy == SortBy.SubmittedDate ? OfficerDashboardSortBy.SubmittedDate : OfficerDashboardSortBy.LastUpdated,
                SortDescending = sortDescending ?? false,
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10
            };

            var result = await _mediator.Send(query, default);
            return Ok(result);
        }
        
        public override async Task<ActionResult<OfficerApplicationReviewResponse>> GetOfficerApplicationReview(Guid applicationId)
        {
            var result = await _mediator.Send(new GetOfficerApplicationReviewQuery { ApplicationId = applicationId });
            if (result is null)
                return NotFound();
            return Ok(result.ToResponse());
        }


        private static List<ApplicationStatus>? ParseStatuses(string? statuses)
        {
            if (string.IsNullOrWhiteSpace(statuses)) return null;
            var result = new List<ApplicationStatus>();
            foreach (var part in statuses.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<ApplicationStatus>(part.Trim(), out var s))
                    result.Add(s);
            }
            return result.Count > 0 ? result : null;
        }

        public override async Task<ActionResult<ICollection<ApplicationActivity>>> GetApplicationActivity(Guid applicationId)
        {
            var query = new GetApplicationActivityQuery { ApplicationId = applicationId };
            var result = await _mediator.Send(query, default);
            return Ok(result.Select(a => a.ToResponse()).ToList());
        }
    }
}
