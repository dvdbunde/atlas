using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ATLAS.Application.Commands;
using ATLAS.Application.Queries;
using ATLAS.Application.DTOs;
using ATLAS.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        
        public ApplicationsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
        
        #region GET /api/applications
        [HttpGet]
        [Authorize(Roles = "Citizen,Officer,Admin")]
        public async Task<ActionResult<IEnumerable<ApplicationSummaryDto>>> GetApplications(
            [FromQuery] Guid? citizenId,
            [FromQuery] Guid? officerId,
            [FromQuery] string? status,
            [FromQuery] Guid? permitTypeId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            var query = new GetApplicationsQuery
            {
                CitizenId = citizenId,
                OfficerId = officerId,
                Status = status,
                PermitTypeId = permitTypeId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Search = search
            };
            
            var results = await _mediator.Send(query, cancellationToken);
            return Ok(results);
        }
        #endregion

        #region POST /api/applications
        [HttpPost]
        [Authorize(Roles = "Citizen,Officer,Admin")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Guid>> SubmitApplication(
            [FromBody] SubmitApplicationCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var applicationId = await _mediator.Send(command, cancellationToken);
                
                return CreatedAtAction(
                    actionName: nameof(GetApplicationById),
                    routeValues: new { id = applicationId },
                    value: applicationId);
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred",
                    Errors = ex.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
            }
        }
        #endregion

        #region GET /api/applications/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Citizen,Officer,Admin")]
        [ProducesResponseType(typeof(ApplicationDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApplicationDetailDto>> GetApplicationById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var query = new GetApplicationByIdQuery { ApplicationId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound();
                
            return Ok(result);
        }
        #endregion

        #region POST /api/applications/{id}/approve
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Officer,Admin")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> ApproveApplication(
            [FromRoute] Guid id,
            [FromBody] ApproveApplicationCommand command,
            CancellationToken cancellationToken)
        {
            command.ApplicationId = id;
            
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                
                if (!result)
                    return NotFound();
                    
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Status Transition",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message
                });
            }
        }
        #endregion

        #region POST /api/applications/{id}/reject
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Officer,Admin")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> RejectApplication(
            [FromRoute] Guid id,
            [FromBody] RejectApplicationCommand command,
            CancellationToken cancellationToken)
        {
            command.ApplicationId = id;
            
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                
                if (!result)
                    return NotFound();
                    
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Status Transition",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message
                });
            }
        }
        #endregion

        #region POST /api/applications/{id}/request-info
        [HttpPost("{id}/request-info")]
        [Authorize(Roles = "Officer,Admin")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> RequestInfo(
            [FromRoute] Guid id,
            [FromBody] RequestInfoCommand command,
            CancellationToken cancellationToken)
        {
            command.ApplicationId = id;
            
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                
                if (!result)
                    return NotFound();
                    
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Status Transition",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message
                });
            }
        }
        #endregion

        #region POST /api/applications/{id}/assign
        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Officer,Admin")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> AssignToOfficer(
            [FromRoute] Guid id,
            [FromBody] AssignToOfficerCommand command,
            CancellationToken cancellationToken)
        {
            command.ApplicationId = id;
            
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                
                if (!result)
                    return NotFound();
                    
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Status Transition",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message
                });
            }
        }
        #endregion
    }
}
