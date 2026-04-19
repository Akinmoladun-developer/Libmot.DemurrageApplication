using Libmot.DemurrageApplicationAPI.BackgroundJobs;
using Libmot.DemurrageApplicationAPI.DTOs.Demurrage;
using Libmot.DemurrageApplicationAPI.DTOs.RateConfig;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using Libmot.DemurrageApplicationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hangfire;

namespace Libmot.DemurrageApplicationAPI.Controllers
{
    [ApiController]
    [Route("api/demurrage")]
    [Authorize]
    [Produces("application/json")]
    public class DemurrageController : ControllerBase
    {
        private readonly IDemurrageService _demurrageService;

        public DemurrageController(IDemurrageService demurrageService)
        {
            _demurrageService = demurrageService;
        }

        // ACCRUAL QUERIES
        // Get full demurrage summary with daily breakdown for a shipment.
        [HttpGet("summary/{shipmentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<DemurrageSummaryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DemurrageSummaryDto>), 404)]
        public async Task<IActionResult> GetSummary(int shipmentId)
        {
            var response = await _demurrageService.GetSummaryAsync(shipmentId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        
        // Get paged list of all demurrage accrual records with filters.
        [HttpGet("accruals")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DemurrageAccrualResponseDto>>), 200)]
        public async Task<IActionResult> GetAccruals([FromQuery] DemurrageFilterDto filter)
        {
            var response = await _demurrageService.GetAccrualsAsync(filter);
            return Ok(response);
        }

        // RATE MANAGEMENT
        // Get all demurrage rate tiers.
        [HttpGet("rates")]
        [ProducesResponseType(typeof(ApiResponse<List<DemurrageRateResponseDto>>), 200)]
        public async Task<IActionResult> GetRates()
        {
            var response = await _demurrageService.GetRatesAsync();
            return Ok(response);
        }

        
        // Create a new demurrage rate tier. SuperAdmin only.
        [HttpPost("rates")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<DemurrageRateResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DemurrageRateResponseDto>), 400)]
        public async Task<IActionResult> CreateRate([FromBody] CreateDemurrageRateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _demurrageService.CreateRateAsync(dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

      
        // Update an existing rate tier. SuperAdmin only.
        [HttpPut("rates/{rateId:int}")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<DemurrageRateResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DemurrageRateResponseDto>), 400)]
        public async Task<IActionResult> UpdateRate(int rateId, [FromBody] UpdateDemurrageRateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _demurrageService.UpdateRateAsync(rateId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Delete or deactivate a rate tier. SuperAdmin only.
        [HttpDelete("rates/{rateId:int}")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<IActionResult> DeleteRate(int rateId)
        {
            var response = await _demurrageService.DeleteRateAsync(rateId);
            return response.Success ? Ok(response) : BadRequest(response);
        }


        // MANUAL JOB TRIGGER (SuperAdmin — for testing & recovery)
        // Manually trigger the demurrage accrual job. Use for testing or to recover from a missed midnight run.
        // SuperAdmin only.
        [HttpPost("run-job")]
        [Authorize(Policy = "SuperAdmin")]  
        [ProducesResponseType(typeof(ApiResponse<DemurrageJobResultDto>), 200)]
        public async Task<IActionResult> RunJobManually()
        {
            var result = await _demurrageService.RunDailyAccrualJobAsync();

            return Ok(ApiResponse<DemurrageJobResultDto>.Ok(
                result, "Demurrage accrual job completed."));
        }

        // Enqueue the accrual job via Hangfire (runs in background).
        // SuperAdmin only.
        [HttpPost("enqueue-job")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(200)]
        public IActionResult EnqueueJob()
        {
            var jobId = BackgroundJob.Enqueue<DemurrageAccrualJob>(j => j.ExecuteAsync());

            return Ok(ApiResponse<string>.Ok(jobId,
                "Demurrage accrual job enqueued in Hangfire."));
        }
    }
}
