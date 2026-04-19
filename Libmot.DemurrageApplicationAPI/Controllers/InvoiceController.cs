using Libmot.DemurrageApplicationAPI.DTOs.Invoice;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using Libmot.DemurrageApplicationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libmot.DemurrageApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Get all invoices — paged and filtered.
        /// Customers see only their own.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceListDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] InvoiceFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var response = await _invoiceService.GetAllAsync(filter, userId, userRole);
            return Ok(response);
        }

        /// <summary>
        /// Get invoice by ID with full line-item breakdown.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _invoiceService.GetByIdAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Get invoice by invoice number.
        /// </summary>
        [HttpGet("number/{invoiceNumber}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 404)]
        public async Task<IActionResult> GetByNumber(string invoiceNumber)
        {
            var response = await _invoiceService.GetByNumberAsync(invoiceNumber);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Manually create a demurrage invoice for a shipment.
        /// Finance Officer and SuperAdmin only.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _invoiceService.CreateManualAsync(dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Update invoice due date or notes.
        /// Finance Officer and SuperAdmin only.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<InvoiceResponseDto>), 400)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var response = await _invoiceService.UpdateAsync(id, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Cancel an unpaid invoice. SuperAdmin only.
        /// </summary>
        [HttpPatch("{id:int}/cancel")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _invoiceService.CancelAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Generate and save the invoice PDF to disk.
        /// Returns the saved file path.
        /// </summary>
        [HttpPost("{id:int}/generate-pdf")]
        [Authorize(Policy = "FinanceOfficer")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GeneratePdf(int id)
        {
            var response = await _invoiceService.GeneratePdfAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Download invoice as PDF file directly.
        /// Always regenerates to reflect latest amounts.
        /// </summary>
        [HttpGet("{id:int}/download-pdf")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<byte[]>), 404)]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var response = await _invoiceService.DownloadPdfAsync(id);
            if (!response.Success) return NotFound(response);

            return File(
                response.Data!,
                "application/pdf",
                $"Invoice-{id}.pdf");
        }

        /// <summary>
        /// Manually mark overdue invoices. SuperAdmin only.
        /// Normally called by the scheduled job.
        /// </summary>
        [HttpPost("mark-overdue")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> MarkOverdue()
        {
            var response = await _invoiceService.MarkOverdueAsync();
            return Ok(response);
        }
    }
}
