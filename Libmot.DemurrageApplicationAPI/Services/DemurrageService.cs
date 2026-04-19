using Libmot.DemurrageApplication.Data;
using Libmot.DemurrageApplication.Models;
using Libmot.DemurrageApplicationAPI.DTOs.Demurrage;
using Libmot.DemurrageApplicationAPI.DTOs.RateConfig;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using LibmotExpress.DemurrageApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Libmot.DemurrageApplicationAPI.Services
{

    public interface IDemurrageService
    {
        // Daily job entry point
        Task<DemurrageJobResultDto> RunDailyAccrualJobAsync();

        // Queries
        Task<ApiResponse<DemurrageSummaryDto>> GetSummaryAsync(int shipmentId);
        Task<ApiResponse<PagedResult<DemurrageAccrualResponseDto>>> GetAccrualsAsync(DemurrageFilterDto filter);

        // Rate management
        Task<ApiResponse<List<DemurrageRateResponseDto>>> GetRatesAsync();
        Task<ApiResponse<DemurrageRateResponseDto>> CreateRateAsync(CreateDemurrageRateDto dto, string createdByUserId);
        Task<ApiResponse<DemurrageRateResponseDto>> UpdateRateAsync(int rateId, UpdateDemurrageRateDto dto);
        Task<ApiResponse<bool>> DeleteRateAsync(int rateId);
    }
    public class DemurrageService : IDemurrageService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DemurrageService> _logger;
        private const int FREE_DAYS = 7;

        // WAT timezone (UTC+1) — Nigeria has no DST
        private static readonly TimeZoneInfo WAT =
            TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");

        public DemurrageService(AppDbContext db, ILogger<DemurrageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        
        // DAILY ACCRUAL JOB — runs at 23:00 UTC (midnight WAT)
        public async Task<DemurrageJobResultDto> RunDailyAccrualJobAsync()
        {
            var jobResult = new DemurrageJobResultDto { JobRunAt = DateTime.UtcNow };
            var watToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WAT).Date;

            _logger.LogInformation(
                "Demurrage accrual job started at {UtcNow} (WAT date: {WatDate})",
                DateTime.UtcNow, watToday);

            // Load all active rate bands once
            var activeRates = await _db.DemurrageRates
                .Where(r => r.IsActive
                         && r.EffectiveFrom.Date <= watToday
                         && (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= watToday))
                .OrderBy(r => r.DayFrom)
                .ToListAsync();

            if (!activeRates.Any())
            {
                var msg = "No active demurrage rate bands found. Job aborted.";
                _logger.LogWarning(msg);
                jobResult.Errors.Add(msg);
                return jobResult;
            }

            // Fetch all shipments eligible for demurrage processing:
            // - Arrived at destination OR already in DemurrageActive status
            // - Not yet picked up or closed
            var eligibleShipments = await _db.Shipments
                .Include(s => s.Customer).ThenInclude(c => c.User)
                .Where(s => s.ActualArrivalDate.HasValue
                         && (s.Status == ShipmentStatus.ArrivedAtDestination
                          || s.Status == ShipmentStatus.DemurrageActive)
                         && s.PickedUpAt == null)
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} eligible shipments to process.", eligibleShipments.Count);

            foreach (var shipment in eligibleShipments)
            {
                try
                {
                    await ProcessShipmentAsync(
                        shipment, activeRates, watToday, jobResult);
                }
                catch (Exception ex)
                {
                    var err = $"Error processing shipment {shipment.WaybillNumber}: {ex.Message}";
                    _logger.LogError(ex, err);
                    jobResult.Errors.Add(err);
                }
            }

            _logger.LogInformation(
                "Demurrage job complete. Processed: {S}, Accruals: {A}, Invoices: {I}, Amount: ₦{M:N2}",
                jobResult.ShipmentsProcessed,
                jobResult.NewAccrualsCreated,
                jobResult.InvoicesGenerated,
                jobResult.TotalAmountAccrued);

            return jobResult;
        }

        // Process Each Shipment
        private async Task ProcessShipmentAsync(
            Shipment shipment,
            List<DemurrageRate> activeRates,
            DateTime watToday,
            DemurrageJobResultDto jobResult)
        {
            var arrivalDate = shipment.ActualArrivalDate!.Value.Date;
            var dayCount = DemurrageCalculator.GetDayCount(arrivalDate, watToday);

            jobResult.ShipmentsProcessed++;

            // Warning of Free period (Day 5 and 6)
            var demurrageDay = DemurrageCalculator.GetDemurrageDayNumber(arrivalDate, watToday);

            if (dayCount == FREE_DAYS - 2 || dayCount == FREE_DAYS - 1)
            {
                // Notification will be queued — wired in Phase 7
                _logger.LogInformation(
                    "Shipment {W}: free days expiring in {D} day(s).",
                    shipment.WaybillNumber, FREE_DAYS - dayCount + 1);
            }

            // Demurrage within free period 
            if (demurrageDay <= 0)
            {
                _logger.LogInformation(
                    "Shipment {W}: Day {D} — within free period, no accrual.",
                    shipment.WaybillNumber, dayCount);
                return;
            }

            // Calculating tier demurrage for today 
            var tier = DemurrageCalculator.ResolveTier(dayCount, activeRates);
            if (tier is null)
            {
                _logger.LogWarning(
                    "Shipment {W}: Day {D} — no matching rate tier found, skipping.",
                    shipment.WaybillNumber, dayCount);
                return;
            }

            // Skip if today's accrual already posted
            var alreadyPosted = await _db.DemurrageAccruals
                .AnyAsync(a => a.ShipmentId == shipment.Id
                            && a.AccrualDate.Date == watToday);

            if (alreadyPosted)
            {
                _logger.LogInformation(
                    "Shipment {W}: accrual for {D:dd-MMM-yyyy} already posted, skipping.",
                    shipment.WaybillNumber, watToday);
                return;
            }

            // Get the total running accumulated demurrage
            var previousTotal = await _db.DemurrageAccruals
                .Where(a => a.ShipmentId == shipment.Id)
                .OrderByDescending(a => a.AccrualDate)
                .Select(a => (decimal?)a.AccumulatedTotal)
                .FirstOrDefaultAsync() ?? 0m;

            var newTotal = previousTotal + tier.DailyRate;

            // Get Post today's accrual
            var accrual = new DemurrageAccrual
            {
                ShipmentId = shipment.Id,
                DemurrageRateId = tier.Id,
                AccrualDate = watToday,
                DayCount = dayCount,
                DailyRateApplied = tier.DailyRate,
                AccumulatedTotal = newTotal,
                IsSettled = false
            };

            _db.DemurrageAccruals.Add(accrual);

            // ── Update shipment status to DemurrageActive if not already ──
            if (shipment.Status == ShipmentStatus.ArrivedAtDestination)
            {
                shipment.Status = ShipmentStatus.DemurrageActive;
                shipment.IsDemurrageActive = true;

                _logger.LogInformation(
                    "Shipment {W} transitioned to DemurrageActive on Day {D}.",
                    shipment.WaybillNumber, dayCount);
            }

            await _db.SaveChangesAsync();

            jobResult.NewAccrualsCreated++;
            jobResult.TotalAmountAccrued += tier.DailyRate;

            _logger.LogInformation(
                "Shipment {W}: Day {D} — Tier '{T}' — ₦{R:N2}/day — Running total: ₦{Total:N2}",
                shipment.WaybillNumber, dayCount, tier.TierName, tier.DailyRate, newTotal);

            // ── Auto-generate invoice on Day 8 (first demurrage day) ──────
            if (demurrageDay == 1)
            {
                var invoiceCreated = await AutoGenerateInvoiceAsync(shipment, newTotal);
                if (invoiceCreated) jobResult.InvoicesGenerated++;
            }
            else
            {
                // Update existing open invoice total
                await UpdateOpenInvoiceTotalAsync(shipment.Id, newTotal);
            }
        }

        // Auto-generate invoice for merchant for Day 8
        private async Task<bool> AutoGenerateInvoiceAsync(Shipment shipment, decimal amount)
        {
            // Guard: don't duplicate if invoice already exists for this shipment
            var exists = await _db.Invoices
                .AnyAsync(i => i.ShipmentId == shipment.Id
                            && i.Status != InvoiceStatus.Cancelled);
            if (exists) return false;

            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ShipmentId = shipment.Id,
                CustomerId = shipment.CustomerId,
                TotalAmount = amount,
                Status = InvoiceStatus.Unpaid,
                IssuedDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(3), // 3-day payment window
                IsSystemGenerated = true,
                Notes = $"Auto-generated demurrage invoice. " +
                                    $"Waybill: {shipment.WaybillNumber}. " +
                                    $"Free period expired on {shipment.FreeDaysExpiry:dd MMM yyyy}."
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Invoice {N} auto-generated for shipment {W} — Amount: ₦{A:N2}",
                invoiceNumber, shipment.WaybillNumber, amount);

            return true;
        }

        // Update open invoice total as demurrage grows
        private async Task UpdateOpenInvoiceTotalAsync(int shipmentId, decimal newTotal)
        {
            var invoice = await _db.Invoices
                .Where(i => i.ShipmentId == shipmentId
                         && (i.Status == InvoiceStatus.Unpaid
                          || i.Status == InvoiceStatus.Overdue))
                .OrderByDescending(i => i.IssuedDate)
                .FirstOrDefaultAsync();

            if (invoice is null) return;

            invoice.TotalAmount = newTotal;

            // Mark overdue if past due date
            if (invoice.Status == InvoiceStatus.Unpaid
                && invoice.DueDate.Date < DateTime.UtcNow.Date)
                invoice.Status = InvoiceStatus.Overdue;

            await _db.SaveChangesAsync();
        }

        // Generate Invoice number
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
                var lastPart = last.Split('-').Last();
                if (int.TryParse(lastPart, out var lastNum))
                    next = lastNum + 1;
            }

            return $"{prefix}{next:D4}";
        }

        
        // QUERIES
        public async Task<ApiResponse<DemurrageSummaryDto>> GetSummaryAsync(int shipmentId)
        {
            var shipment = await _db.Shipments
                .Include(s => s.Customer).ThenInclude(c => c.User)
                .Include(s => s.DemurrageAccruals)
                    .ThenInclude(a => a.DemurrageRate)
                .FirstOrDefaultAsync(s => s.Id == shipmentId);

            if (shipment is null)
                return ApiResponse<DemurrageSummaryDto>.Fail("Shipment not found.");

            if (!shipment.ActualArrivalDate.HasValue)
                return ApiResponse<DemurrageSummaryDto>.Fail(
                    "Arrival has not been recorded for this shipment.");

            var watToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WAT).Date;
            var endDate = shipment.PickedUpAt?.Date ?? watToday;
            var arrivalDate = shipment.ActualArrivalDate.Value.Date;
            var daysInStorage = (endDate - arrivalDate).Days;
            var demurrageDays = Math.Max(0, daysInStorage - FREE_DAYS);

            var allAccruals = shipment.DemurrageAccruals
                .OrderBy(a => a.AccrualDate)
                .ToList();

            var totalAccrued = allAccruals.Any()
                ? allAccruals.Max(a => a.AccumulatedTotal)
                : 0m;

            var settled = allAccruals
                .Where(a => a.IsSettled)
                .Sum(a => a.DailyRateApplied);

            var summary = new DemurrageSummaryDto
            {
                ShipmentId = shipment.Id,
                WaybillNumber = shipment.WaybillNumber,
                CustomerName = shipment.Customer.User.FullName,
                CustomerCompany = shipment.Customer.CompanyName,
                OriginState = shipment.OriginState,
                DestinationState = shipment.DestinationState,
                ArrivalDate = arrivalDate,
                FreeDaysExpiry = shipment.FreeDaysExpiry!.Value,
                TotalDaysInStorage = daysInStorage,
                FreeDays = FREE_DAYS,
                DemurrageDays = demurrageDays,
                TotalAccruedAmount = totalAccrued,
                SettledAmount = settled,
                OutstandingAmount = totalAccrued - settled,
                ShipmentStatus = shipment.Status.ToString(),
                DailyBreakdown = allAccruals.Select(a => new DemurrageAccrualResponseDto
                {
                    Id = a.Id,
                    ShipmentId = a.ShipmentId,
                    WaybillNumber = shipment.WaybillNumber,
                    CustomerName = shipment.Customer.User.FullName,
                    CustomerCompany = shipment.Customer.CompanyName,
                    AccrualDate = a.AccrualDate,
                    DayCount = a.DayCount,
                    TierApplied = a.DemurrageRate.TierName,
                    DailyRateApplied = a.DailyRateApplied,
                    AccumulatedTotal = a.AccumulatedTotal,
                    IsSettled = a.IsSettled,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };

            return ApiResponse<DemurrageSummaryDto>.Ok(summary);
        }

        public async Task<ApiResponse<PagedResult<DemurrageAccrualResponseDto>>> GetAccrualsAsync(
            DemurrageFilterDto filter)
        {
            var query = _db.DemurrageAccruals
                .Include(a => a.Shipment).ThenInclude(s => s.Customer).ThenInclude(c => c.User)
                .Include(a => a.DemurrageRate)
                .AsQueryable();

            if (filter.ShipmentId.HasValue)
                query = query.Where(a => a.ShipmentId == filter.ShipmentId.Value);

            if (filter.CustomerId.HasValue)
                query = query.Where(a => a.Shipment.CustomerId == filter.CustomerId.Value);

            if (filter.IsSettled.HasValue)
                query = query.Where(a => a.IsSettled == filter.IsSettled.Value);

            if (filter.AccrualDateFrom.HasValue)
                query = query.Where(a => a.AccrualDate.Date >= filter.AccrualDateFrom.Value.Date);

            if (filter.AccrualDateTo.HasValue)
                query = query.Where(a => a.AccrualDate.Date <= filter.AccrualDateTo.Value.Date);

            var total = await query.CountAsync();

            var accruals = await query
                .OrderByDescending(a => a.AccrualDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = accruals.Select(a => new DemurrageAccrualResponseDto
            {
                Id = a.Id,
                ShipmentId = a.ShipmentId,
                WaybillNumber = a.Shipment.WaybillNumber,
                CustomerName = a.Shipment.Customer.User.FullName,
                CustomerCompany = a.Shipment.Customer.CompanyName,
                AccrualDate = a.AccrualDate,
                DayCount = a.DayCount,
                TierApplied = a.DemurrageRate.TierName,
                DailyRateApplied = a.DailyRateApplied,
                AccumulatedTotal = a.AccumulatedTotal,
                IsSettled = a.IsSettled,
                CreatedAt = a.CreatedAt
            }).ToList();

            return ApiResponse<PagedResult<DemurrageAccrualResponseDto>>.Ok(
                new PagedResult<DemurrageAccrualResponseDto>
                {
                    Items = items,
                    TotalCount = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
        }

        
        // RATE MANAGEMENT
        public async Task<ApiResponse<List<DemurrageRateResponseDto>>> GetRatesAsync()
        {
            var rates = await _db.DemurrageRates
                .OrderBy(r => r.DayFrom)
                .ToListAsync();

            return ApiResponse<List<DemurrageRateResponseDto>>.Ok(
                rates.Select(MapRateToDto).ToList());
        }

        public async Task<ApiResponse<DemurrageRateResponseDto>> CreateRateAsync(
            CreateDemurrageRateDto dto, string createdByUserId)
        {
            // Validate no overlap with existing active tiers
            var overlap = await _db.DemurrageRates
                .AnyAsync(r => r.IsActive
                            && r.DayFrom <= (dto.DayTo == 0 ? int.MaxValue : dto.DayTo)
                            && (r.DayTo == 0 || r.DayTo >= dto.DayFrom));

            if (overlap)
                return ApiResponse<DemurrageRateResponseDto>.Fail(
                    "The specified day range overlaps with an existing active tier. " +
                    "Deactivate the conflicting tier first.");

            if (dto.DayFrom <= FREE_DAYS)
                return ApiResponse<DemurrageRateResponseDto>.Fail(
                    $"DayFrom must be greater than {FREE_DAYS} (the free period).");

            if (dto.DayTo != 0 && dto.DayTo < dto.DayFrom)
                return ApiResponse<DemurrageRateResponseDto>.Fail(
                    "DayTo must be greater than or equal to DayFrom, or 0 for open-ended.");

            var rate = new DemurrageRate
            {
                TierName = dto.TierName.Trim(),
                DayFrom = dto.DayFrom,
                DayTo = dto.DayTo,
                DailyRate = dto.DailyRate,
                IsActive = true,
                EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow,
                EffectiveTo = dto.EffectiveTo,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.DemurrageRates.Add(rate);
            await _db.SaveChangesAsync();

            return ApiResponse<DemurrageRateResponseDto>.Ok(
                MapRateToDto(rate), "Rate tier created successfully.");
        }

        public async Task<ApiResponse<DemurrageRateResponseDto>> UpdateRateAsync(
            int rateId, UpdateDemurrageRateDto dto)
        {
            var rate = await _db.DemurrageRates.FindAsync(rateId);
            if (rate is null)
                return ApiResponse<DemurrageRateResponseDto>.Fail("Rate tier not found.");

            if (!string.IsNullOrWhiteSpace(dto.TierName))
                rate.TierName = dto.TierName.Trim();

            if (dto.DailyRate.HasValue)
                rate.DailyRate = dto.DailyRate.Value;

            if (dto.IsActive.HasValue)
                rate.IsActive = dto.IsActive.Value;

            if (dto.EffectiveTo.HasValue)
                rate.EffectiveTo = dto.EffectiveTo.Value;

            await _db.SaveChangesAsync();

            return ApiResponse<DemurrageRateResponseDto>.Ok(
                MapRateToDto(rate), "Rate tier updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteRateAsync(int rateId)
        {
            var rate = await _db.DemurrageRates.FindAsync(rateId);
            if (rate is null)
                return ApiResponse<bool>.Fail("Rate tier not found.");

            var hasAccruals = await _db.DemurrageAccruals
                .AnyAsync(a => a.DemurrageRateId == rateId);

            if (hasAccruals)
            {
                // Soft delete — deactivate instead of removing
                rate.IsActive = false;
                rate.EffectiveTo = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return ApiResponse<bool>.Ok(true,
                    "Rate tier deactivated (cannot delete a tier with existing accruals).");
            }

            _db.DemurrageRates.Remove(rate);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Rate tier deleted successfully.");
        }

        // Map helper
        private static DemurrageRateResponseDto MapRateToDto(DemurrageRate r) => new()
        {
            Id = r.Id,
            TierName = r.TierName,
            DayFrom = r.DayFrom,
            DayTo = r.DayTo,
            DayRangeLabel = r.DayTo == 0
                              ? $"Day {r.DayFrom}+"
                              : $"Day {r.DayFrom}–{r.DayTo}",
            DailyRate = r.DailyRate,
            IsActive = r.IsActive,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            CreatedAt = r.CreatedAt
        };

    }
}
