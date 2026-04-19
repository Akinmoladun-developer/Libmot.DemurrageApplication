using Libmot.DemurrageApplication.Data;
using Libmot.DemurrageApplication.Models;
using Libmot.DemurrageApplicationAPI.DTOs.Invoice;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using LibmotExpress.DemurrageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public interface IInvoiceService
    {
        Task<ApiResponse<InvoiceResponseDto>> GetByIdAsync(int invoiceId);
        Task<ApiResponse<InvoiceResponseDto>> GetByNumberAsync(string invoiceNumber);
        Task<ApiResponse<PagedResult<InvoiceListDto>>> GetAllAsync(InvoiceFilterDto filter, string requestingUserId, string requestingUserRole);
        Task<ApiResponse<InvoiceResponseDto>> CreateManualAsync(CreateInvoiceDto dto, string createdByUserId);
        Task<ApiResponse<InvoiceResponseDto>> UpdateAsync(int invoiceId, UpdateInvoiceDto dto);
        Task<ApiResponse<bool>> CancelAsync(int invoiceId, string cancelledByUserId);
        Task<ApiResponse<string>> GeneratePdfAsync(int invoiceId);
        Task<ApiResponse<byte[]>> DownloadPdfAsync(int invoiceId);
        Task<ApiResponse<bool>> MarkOverdueAsync();   // called by scheduler
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private const int FREE_DAYS = 7;

        public InvoiceService(
            AppDbContext db,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _db = db;
            _env = env;
            _config = config;
        }

        // ── Get by ID ─────────────────────────────────────────────────────
        public async Task<ApiResponse<InvoiceResponseDto>> GetByIdAsync(int invoiceId)
        {
            var invoice = await LoadInvoiceAsync(invoiceId);
            if (invoice is null)
                return ApiResponse<InvoiceResponseDto>.Fail("Invoice not found.");

            return ApiResponse<InvoiceResponseDto>.Ok(await BuildResponseDto(invoice));
        }

        // ── Get by invoice number ─────────────────────────────────────────
        public async Task<ApiResponse<InvoiceResponseDto>> GetByNumberAsync(string invoiceNumber)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber.Trim().ToUpper());

            if (invoice is null)
                return ApiResponse<InvoiceResponseDto>.Fail(
                    $"Invoice '{invoiceNumber}' not found.");

            return ApiResponse<InvoiceResponseDto>.Ok(
                await BuildResponseDto(await LoadInvoiceAsync(invoice.Id)));
        }

        // ── Get all (paged + filtered) ────────────────────────────────────
        public async Task<ApiResponse<PagedResult<InvoiceListDto>>> GetAllAsync(
            InvoiceFilterDto filter, string requestingUserId, string requestingUserRole)
        {
            var query = _db.Invoices
                .Include(i => i.Customer).ThenInclude(c => c.User)
                .Include(i => i.Shipment)
                .Include(i => i.Payments)
                .AsQueryable();

            // Customers see only their own invoices
            if (requestingUserRole == "Customer")
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.UserId == requestingUserId);

                if (customer is null)
                    return ApiResponse<PagedResult<InvoiceListDto>>
                        .Fail("Customer profile not found.");

                query = query.Where(i => i.CustomerId == customer.Id);
            }

            if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
                query = query.Where(i => i.InvoiceNumber.Contains(filter.InvoiceNumber));

            if (filter.CustomerId.HasValue)
                query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

            if (filter.ShipmentId.HasValue)
                query = query.Where(i => i.ShipmentId == filter.ShipmentId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                Enum.TryParse<InvoiceStatus>(filter.Status, true, out var statusEnum))
                query = query.Where(i => i.Status == statusEnum);

            if (filter.IssuedFrom.HasValue)
                query = query.Where(i => i.IssuedDate >= filter.IssuedFrom.Value);

            if (filter.IssuedTo.HasValue)
                query = query.Where(i => i.IssuedDate <= filter.IssuedTo.Value);

            var today = DateTime.UtcNow.Date;

            if (filter.IsOverdue == true)
                query = query.Where(i =>
                    i.DueDate.Date < today &&
                    (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue));

            var total = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.IssuedDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = invoices.Select(i =>
            {
                var paid = i.Payments.Where(p => p.Status == PaymentStatus.Confirmed)
                                        .Sum(p => p.AmountPaid);
                var balance = i.TotalAmount - paid;
                return new InvoiceListDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    WaybillNumber = i.Shipment.WaybillNumber,
                    CustomerCompany = i.Customer.CompanyName,
                    CustomerName = i.Customer.User.FullName,
                    TotalAmount = i.TotalAmount,
                    BalanceRemaining = balance,
                    Status = i.Status.ToString(),
                    IssuedDate = i.IssuedDate,
                    DueDate = i.DueDate,
                    IsOverdue = i.DueDate.Date < today
                                    && i.Status is InvoiceStatus.Unpaid or InvoiceStatus.Overdue
                };
            }).ToList();

            return ApiResponse<PagedResult<InvoiceListDto>>.Ok(
                new PagedResult<InvoiceListDto>
                {
                    Items = items,
                    TotalCount = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
        }

        // ── Manual invoice creation ───────────────────────────────────────
        public async Task<ApiResponse<InvoiceResponseDto>> CreateManualAsync(
            CreateInvoiceDto dto, string createdByUserId)
        {
            var shipment = await _db.Shipments
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == dto.ShipmentId);

            if (shipment is null)
                return ApiResponse<InvoiceResponseDto>.Fail("Shipment not found.");

            if (!shipment.ActualArrivalDate.HasValue)
                return ApiResponse<InvoiceResponseDto>.Fail(
                    "Cannot invoice a shipment that has not arrived yet.");

            if (shipment.Status is ShipmentStatus.PickedUp or
                ShipmentStatus.Closed or ShipmentStatus.Cancelled)
                return ApiResponse<InvoiceResponseDto>.Fail(
                    $"Cannot create invoice for shipment with status '{shipment.Status}'.");

            // Guard against duplicate open invoices
            var openExists = await _db.Invoices
                .AnyAsync(i => i.ShipmentId == dto.ShipmentId
                            && i.Status != InvoiceStatus.Cancelled
                            && i.Status != InvoiceStatus.Paid);

            if (openExists)
                return ApiResponse<InvoiceResponseDto>.Fail(
                    "An open invoice already exists for this shipment. " +
                    "Settle or cancel the existing invoice first.");

            // Calculate demurrage total if no override
            decimal amount;
            if (dto.OverrideAmount.HasValue)
            {
                amount = dto.OverrideAmount.Value;
            }
            else
            {
                var latest = await _db.DemurrageAccruals
                    .Where(a => a.ShipmentId == dto.ShipmentId)
                    .OrderByDescending(a => a.AccrualDate)
                    .FirstOrDefaultAsync();

                amount = latest?.AccumulatedTotal ?? 0m;

                if (amount == 0)
                    return ApiResponse<InvoiceResponseDto>.Fail(
                        "No demurrage accrual found for this shipment. " +
                        "Provide an OverrideAmount or wait for the accrual job to run.");
            }

            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ShipmentId = dto.ShipmentId,
                CustomerId = shipment.CustomerId,
                TotalAmount = amount,
                Status = InvoiceStatus.Unpaid,
                IssuedDate = DateTime.UtcNow,
                DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(3),
                IsSystemGenerated = false,
                Notes = dto.Notes
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            return ApiResponse<InvoiceResponseDto>.Ok(
                await BuildResponseDto(await LoadInvoiceAsync(invoice.Id)),
                "Invoice created successfully.");
        }

        // ── Update ────────────────────────────────────────────────────────
        public async Task<ApiResponse<InvoiceResponseDto>> UpdateAsync(
            int invoiceId, UpdateInvoiceDto dto)
        {
            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice is null)
                return ApiResponse<InvoiceResponseDto>.Fail("Invoice not found.");

            if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
                return ApiResponse<InvoiceResponseDto>.Fail(
                    $"Cannot edit a '{invoice.Status}' invoice.");

            if (dto.DueDate.HasValue)
                invoice.DueDate = dto.DueDate.Value;

            if (dto.Notes is not null)
                invoice.Notes = dto.Notes;

            await _db.SaveChangesAsync();

            return ApiResponse<InvoiceResponseDto>.Ok(
                await BuildResponseDto(await LoadInvoiceAsync(invoiceId)),
                "Invoice updated successfully.");
        }

        // ── Cancel ────────────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> CancelAsync(int invoiceId, string cancelledByUserId)
        {
            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice is null)
                return ApiResponse<bool>.Fail("Invoice not found.");

            if (invoice.Status == InvoiceStatus.Paid)
                return ApiResponse<bool>.Fail("Cannot cancel a paid invoice.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ApiResponse<bool>.Fail("Invoice is already cancelled.");

            invoice.Status = InvoiceStatus.Cancelled;
            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Invoice cancelled.");
        }

        // ── Generate & save PDF ───────────────────────────────────────────
        public async Task<ApiResponse<string>> GeneratePdfAsync(int invoiceId)
        {
            var invoice = await LoadInvoiceAsync(invoiceId);
            if (invoice is null)
                return ApiResponse<string>.Fail("Invoice not found.");

            var dto = await BuildResponseDto(invoice);
            var pdfBytes = InvoicePdfGenerator.Generate(dto);

            var pdfFolder = Path.Combine(
                _env.ContentRootPath,
                _config["StorageSettings:InvoicePdfPath"] ?? "Uploads/Invoices");

            Directory.CreateDirectory(pdfFolder);

            var fileName = $"{invoice.InvoiceNumber}.pdf";
            var filePath = Path.Combine(pdfFolder, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            // Store relative path
            invoice.PdfPath = Path.Combine(
                _config["StorageSettings:InvoicePdfPath"] ?? "Uploads/Invoices",
                fileName);

            await _db.SaveChangesAsync();

            return ApiResponse<string>.Ok(invoice.PdfPath, "PDF generated successfully.");
        }

        // ── Download PDF bytes ────────────────────────────────────────────
        public async Task<ApiResponse<byte[]>> DownloadPdfAsync(int invoiceId)
        {
            var invoice = await LoadInvoiceAsync(invoiceId);
            if (invoice is null)
                return ApiResponse<byte[]>.Fail("Invoice not found.");

            // Always regenerate to ensure latest amounts
            var dto = await BuildResponseDto(invoice);
            var pdfBytes = InvoicePdfGenerator.Generate(dto);

            return ApiResponse<byte[]>.Ok(pdfBytes);
        }

        // ── Mark overdue (called by scheduler) ───────────────────────────
        public async Task<ApiResponse<bool>> MarkOverdueAsync()
        {
            var today = DateTime.UtcNow.Date;

            var overdueInvoices = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Unpaid
                         && i.DueDate.Date < today)
                .ToListAsync();

            foreach (var inv in overdueInvoices)
                inv.Status = InvoiceStatus.Overdue;

            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true,
                $"{overdueInvoices.Count} invoice(s) marked as overdue.");
        }

        // ══════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════

        private async Task<Invoice?> LoadInvoiceAsync(int invoiceId) =>
            await _db.Invoices
                .Include(i => i.Shipment)
                .Include(i => i.Customer).ThenInclude(c => c.User)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

        private async Task<InvoiceResponseDto> BuildResponseDto(Invoice i)
        {
            // Accruals for line items
            var accruals = await _db.DemurrageAccruals
                .Include(a => a.DemurrageRate)
                .Where(a => a.ShipmentId == i.ShipmentId)
                .OrderBy(a => a.AccrualDate)
                .ToListAsync();

            var confirmedPayments = i.Payments
                .Where(p => p.Status == PaymentStatus.Confirmed)
                .ToList();

            var amountPaid = confirmedPayments.Sum(p => p.AmountPaid);
            var balanceRemaining = i.TotalAmount - amountPaid;

            // Build tier-grouped line items
            var lineItems = accruals
                .GroupBy(a => new { a.DemurrageRate.TierName, a.DailyRateApplied })
                .Select(g =>
                {
                    var sorted = g.OrderBy(x => x.AccrualDate).ToList();
                    var dateRange = sorted.Count == 1
                        ? sorted[0].AccrualDate.ToString("dd MMM yyyy")
                        : $"{sorted.First().AccrualDate:dd MMM yyyy} – {sorted.Last().AccrualDate:dd MMM yyyy}";

                    return new InvoiceLineItemDto
                    {
                        TierName = g.Key.TierName,
                        DateRange = dateRange,
                        Days = g.Count(),
                        DailyRate = g.Key.DailyRateApplied,
                        LineTotal = g.Key.DailyRateApplied * g.Count()
                    };
                }).ToList();

            var demurrageDays = accruals.Count;

            return new InvoiceResponseDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                ShipmentId = i.ShipmentId,
                WaybillNumber = i.Shipment.WaybillNumber,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer.User.FullName,
                CustomerCompany = i.Customer.CompanyName,
                CustomerPhone = i.Customer.PhoneNumber,
                CustomerEmail = i.Customer.Email,
                OriginState = i.Shipment.OriginState,
                DestinationState = i.Shipment.DestinationState,
                ItemDescription = i.Shipment.ItemDescription,
                ArrivalDate = i.Shipment.ActualArrivalDate ?? i.IssuedDate,
                FreeDaysExpiry = i.Shipment.FreeDaysExpiry ?? i.IssuedDate,
                DemurrageDays = demurrageDays,
                TotalAmount = i.TotalAmount,
                AmountPaid = amountPaid,
                BalanceRemaining = balanceRemaining,
                Status = i.Status.ToString(),
                IssuedDate = i.IssuedDate,
                DueDate = i.DueDate,
                PaidDate = i.PaidDate,
                IsSystemGenerated = i.IsSystemGenerated,
                Notes = i.Notes,
                PdfPath = i.PdfPath,
                LineItems = lineItems,
                Payments = confirmedPayments.Select(p => new InvoicePaymentDto
                {
                    Id = p.Id,
                    AmountPaid = p.AmountPaid,
                    PaymentMethod = p.PaymentMethod.ToString(),
                    TransactionRef = p.TransactionReference,
                    PaymentDate = p.PaymentDate
                }).ToList()
            };
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var prefix = $"LMX-INV-{DateTime.UtcNow:yyyyMM}-";
            var last = await _db.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last is not null)
            {
                var part = last.Split('-').Last();
                if (int.TryParse(part, out var n)) next = n + 1;
            }

            return $"{prefix}{next:D4}";
        }

    }
}
