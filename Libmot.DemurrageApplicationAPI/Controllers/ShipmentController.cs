using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using Libmot.DemurrageApplicationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Libmot.DemurrageApplicationAPI.Controllers
{

    [ApiController]
    [Route("api/shipments")]
    [Authorize]
    [Produces("application/json")]
    public class ShipmentController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

       
        // Create a new shipment. Finance Officer and SuperAdmin only.
        [HttpPost]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateShipmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _shipmentService.CreateAsync(dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Get all shipments with filters and pagination.
        // Customers see only their own. Drivers see only their assigned ones.
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ShipmentListResponseDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] ShipmentFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var response = await _shipmentService.GetAllAsync(filter, userId, userRole);
            return Ok(response);
        }

        //Get a single shipment by ID.
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _shipmentService.GetByIdAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        //Track a shipment by waybill number. Accessible publicly for customer tracking.
        [HttpGet("track/{waybillNumber}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 404)]
        public async Task<IActionResult> Track(string waybillNumber)
        {
            var response = await _shipmentService.GetByWaybillAsync(waybillNumber);
            return response.Success ? Ok(response) : NotFound(response);
        }

        // Update shipment details. Finance Officer and SuperAdmin only.
        [HttpPut("{id:int}")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateShipmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _shipmentService.UpdateAsync(id, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Assign a driver to a shipment.
        [HttpPatch("{id:int}/assign-driver")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _shipmentService.AssignDriverAsync(id, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Mark a shipment as In-Transit. Driver and above.
        [HttpPatch("{id:int}/in-transit")]
        [Authorize(Policy = "Driver")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> MarkInTransit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _shipmentService.MarkInTransitAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Record arrival of a shipment at its destination.
        // This starts the 7-day free period passes. Record arrival start calculating from the eight(8) day
        [HttpPatch("{id:int}/record-arrival")]
        [Authorize(Policy = "Driver")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> RecordArrival(int id, [FromBody] RecordArrivalDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _shipmentService.RecordArrivalAsync(id, dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Record customer pickup. Stops demurrage accrual.
        [HttpPatch("{id:int}/record-pickup")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ShipmentResponseDto>), 400)]
        public async Task<IActionResult> RecordPickup(int id, [FromBody] RecordPickupDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _shipmentService.RecordPickupAsync(id, dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Cancel a shipment. SuperAdmin only.
        [HttpPatch("{id:int}/cancel")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _shipmentService.CancelAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
