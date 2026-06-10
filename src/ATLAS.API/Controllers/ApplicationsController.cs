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
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public override async Task<ActionResult<ICollection<ApplicationSummaryResponse>>> ApplicationsGet(
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

        public override async Task<ActionResult<Guid>> ApplicationsPost(
            SubmitApplicationRequest body)
        {
            var command = new SubmitApplicationCommand
            {
                CitizenId = body.CitizenId,
                PermitTypeId = body.PermitTypeId,
                CitizenNotes = body.CitizenNotes
            };

             var applicationId = await _mediator.Send(command, default);
    
            // 🆕 Return 201 Created with location header
            return CreatedAtAction(
                nameof(ApplicationsGet), 
                new { id = applicationId }, 
                applicationId);    
        }

        public override async Task<ActionResult<ApplicationDetailResponse>> ApplicationsGet(
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

        public override async Task<ActionResult<bool>> Approve(
            Guid id, ApproveApplicationRequest body)
        {
            var command = new ApproveApplicationCommand
            {
                ApplicationId = id,
                OfficerId = body.OfficerId,
                Comments = body.Comments
            };

            var result = await _mediator.Send(command, default);    
    
            if (!result)
            {
                return NotFound(); // ← 404 if application not found
            }
            
            return Ok(true); // ← 200 OK with true            
        }

        public override async Task<ActionResult<bool>> Reject(
            Guid id, RejectApplicationRequest body)
        {
            var command = new RejectApplicationCommand
            {                
                ApplicationId = id,
                OfficerId = body.OfficerId,
                ReasonCode = body.ReasonCode,
                Comments = body.Comments
            };

            var result = await _mediator.Send(command, default);

            if (!result)
            {
                return NotFound();
            }
    
            return Ok(true);            
        }

        public override async Task<ActionResult<bool>> RequestInfo(
            Guid id, RequestInfoRequest body)
        {
            var command = new RequestInfoCommand
            {
                ApplicationId = id,
                OfficerId = body.OfficerId,
                Message = body.Message
            };

            var result = await _mediator.Send(command, default);
    
            if (!result)
            {
                return NotFound();
            }
            
            return Ok(true);
        }

        public override async Task<ActionResult<bool>> Assign(
            Guid id, AssignToOfficerRequest body)
        {
            var command = new AssignToOfficerCommand
            {
                ApplicationId = id,
                OfficerId = body.OfficerId
            };

            var result = await _mediator.Send(command, default);
    
            if (!result)
            {
                return NotFound();
            }
            
            return Ok(true);
        }
    }
}
