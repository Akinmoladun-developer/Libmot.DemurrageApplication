using Libmot.DemurrageApplication.Data;
using Libmot.DemurrageApplication.Models;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Helpers;
using LibmotExpress.DemurrageApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public interface IShipmentService
    {
        Task<ApiResponse<ShipmentResponseDto>> CreateAsync(CreateShipmentDto dto, string createdByUserId);
        Task<ApiResponse<ShipmentResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<ShipmentResponseDto>> GetByWaybillAsync(string waybillNumber);
        Task<ApiResponse<PagedResult<ShipmentListResponseDto>>> GetAllAsync(ShipmentFilterDto filter, string requestingUserId, string requestingUserRole);
        Task<ApiResponse<ShipmentResponseDto>> UpdateAsync(int id, UpdateShipmentDto dto);
        Task<ApiResponse<ShipmentResponseDto>> AssignDriverAsync(int id, AssignDriverDto dto);
        Task<ApiResponse<ShipmentResponseDto>> MarkInTransitAsync(int id, string updatedByUserId);
        Task<ApiResponse<ShipmentResponseDto>> RecordArrivalAsync(int id, RecordArrivalDto dto, string updatedByUserId);
        Task<ApiResponse<ShipmentResponseDto>> RecordPickupAsync(int id, RecordPickupDto dto, string updatedByUserId);
        Task<ApiResponse<bool>> CancelAsync(int id, string cancelledByUserId);
    }

    public class ShipmentService : IShipmentService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int FREE_DAYS = 7;

        public ShipmentService(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── Create ────────────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> CreateAsync(
            CreateShipmentDto dto, string createdByUserId)
        {
            // Validate states
            if (!NigerianStates.IsValid(dto.OriginState))
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"'{dto.OriginState}' is not a valid Nigerian state.");

            if (!NigerianStates.IsValid(dto.DestinationState))
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"'{dto.DestinationState}' is not a valid Nigerian state.");

            if (dto.OriginState.Equals(dto.DestinationState, StringComparison.OrdinalIgnoreCase))
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Origin and destination states cannot be the same.");

            // Validate customer
            var customer = await _db.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsActive);

            if (customer is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Customer not found or inactive.");

            // Validate driver if provided
            ApplicationUser? driver = null;
            if (!string.IsNullOrWhiteSpace(dto.DriverUserId))
            {
                driver = await _userManager.FindByIdAsync(dto.DriverUserId);
                if (driver is null || driver.Role != "Driver")
                    return ApiResponse<ShipmentResponseDto>.Fail(
                        "Assigned user is not a valid Driver.");
            }

            // Generate unique waybill (retry on collision)
            string waybill;
            do { waybill = WaybillGenerator.Generate(); }
            while (await _db.Shipments.AnyAsync(s => s.WaybillNumber == waybill));

            var shipment = new Shipment
            {
                WaybillNumber = waybill,
                CustomerId = dto.CustomerId,
                DriverUserId = dto.DriverUserId,
                OriginState = dto.OriginState.Trim(),
                DestinationState = dto.DestinationState.Trim(),
                ItemDescription = dto.ItemDescription.Trim(),
                DeclaredValue = dto.DeclaredValue,
                Weight = dto.Weight,
                Status = ShipmentStatus.Pending,
                ExpectedArrivalDate = dto.ExpectedArrivalDate,
                CreatedByUserId = createdByUserId
            };

            _db.Shipments.Add(shipment);
            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(shipment.Id),
                "Shipment created successfully.");
        }

        // ── Get by ID ─────────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> GetByIdAsync(int id)
        {
            var exists = await _db.Shipments.AnyAsync(s => s.Id == id);
            if (!exists)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            return ApiResponse<ShipmentResponseDto>.Ok(await BuildResponseDto(id));
        }

        // ── Get by Waybill ────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> GetByWaybillAsync(string waybillNumber)
        {
            var shipment = await _db.Shipments
                .FirstOrDefaultAsync(s => s.WaybillNumber == waybillNumber.Trim().ToUpper());

            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"No shipment found with waybill number '{waybillNumber}'.");

            return ApiResponse<ShipmentResponseDto>.Ok(await BuildResponseDto(shipment.Id));
        }

        // ── Get all (paged + filtered) ────────────────────────────────────
        public async Task<ApiResponse<PagedResult<ShipmentListResponseDto>>> GetAllAsync(
            ShipmentFilterDto filter, string requestingUserId, string requestingUserRole)
        {
            var query = _db.Shipments
                .Include(s => s.Customer).ThenInclude(c => c.User)
                .AsQueryable();

            // Customers can only see their own shipments
            if (requestingUserRole == "Customer")
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.UserId == requestingUserId);

                if (customer is null)
                    return ApiResponse<PagedResult<ShipmentListResponseDto>>
                        .Fail("Customer profile not found.");

                query = query.Where(s => s.CustomerId == customer.Id);
            }

            // Drivers see only their assigned shipments
            if (requestingUserRole == "Driver")
                query = query.Where(s => s.DriverUserId == requestingUserId);

            // Filters
            if (!string.IsNullOrWhiteSpace(filter.WaybillNumber))
                query = query.Where(s => s.WaybillNumber.Contains(filter.WaybillNumber));

            if (filter.CustomerId.HasValue)
                query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Status) &&
                Enum.TryParse<ShipmentStatus>(filter.Status, true, out var statusEnum))
                query = query.Where(s => s.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(filter.OriginState))
                query = query.Where(s => s.OriginState.Contains(filter.OriginState));

            if (!string.IsNullOrWhiteSpace(filter.DestinationState))
                query = query.Where(s => s.DestinationState.Contains(filter.DestinationState));

            if (filter.IsDemurrageActive.HasValue)
                query = query.Where(s => s.IsDemurrageActive == filter.IsDemurrageActive.Value);

            if (filter.CreatedFrom.HasValue)
                query = query.Where(s => s.CreatedAt >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                query = query.Where(s => s.CreatedAt <= filter.CreatedTo.Value);

            var totalCount = await query.CountAsync();

            var shipments = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Pull accrued amounts in one query
            var shipmentIds = shipments.Select(s => s.Id).ToList();
            var accruedMap = await _db.DemurrageAccruals
                .Where(a => shipmentIds.Contains(a.ShipmentId) && !a.IsSettled)
                .GroupBy(a => a.ShipmentId)
                .Select(g => new { ShipmentId = g.Key, Total = g.Max(a => a.AccumulatedTotal) })
                .ToDictionaryAsync(x => x.ShipmentId, x => x.Total);

            var items = shipments.Select(s => new ShipmentListResponseDto
            {
                Id = s.Id,
                WaybillNumber = s.WaybillNumber,
                CustomerName = s.Customer.User.FullName,
                CustomerCompany = s.Customer.CompanyName,
                OriginState = s.OriginState,
                DestinationState = s.DestinationState,
                Status = s.Status.ToString(),
                CreatedAt = s.CreatedAt,
                ActualArrivalDate = s.ActualArrivalDate,
                FreeDaysExpiry = s.FreeDaysExpiry,
                IsDemurrageActive = s.IsDemurrageActive,
                AccruedDemurrageAmount = accruedMap.GetValueOrDefault(s.Id, 0)
            }).ToList();

            var result = new PagedResult<ShipmentListResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<ShipmentListResponseDto>>.Ok(result);
        }

        // ── Update ────────────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> UpdateAsync(
            int id, UpdateShipmentDto dto)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status is ShipmentStatus.PickedUp or
                ShipmentStatus.Closed or ShipmentStatus.Cancelled)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"Cannot edit a shipment with status '{shipment.Status}'.");

            if (!string.IsNullOrWhiteSpace(dto.DriverUserId))
            {
                var driver = await _userManager.FindByIdAsync(dto.DriverUserId);
                if (driver is null || driver.Role != "Driver")
                    return ApiResponse<ShipmentResponseDto>.Fail(
                        "Assigned user is not a valid Driver.");
                shipment.DriverUserId = dto.DriverUserId;
            }

            if (!string.IsNullOrWhiteSpace(dto.ItemDescription))
                shipment.ItemDescription = dto.ItemDescription.Trim();

            if (dto.DeclaredValue.HasValue)
                shipment.DeclaredValue = dto.DeclaredValue.Value;

            if (dto.Weight.HasValue)
                shipment.Weight = dto.Weight.Value;

            if (dto.ExpectedArrivalDate.HasValue)
                shipment.ExpectedArrivalDate = dto.ExpectedArrivalDate.Value;

            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(id), "Shipment updated successfully.");
        }

        // ── Assign Driver ─────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> AssignDriverAsync(
            int id, AssignDriverDto dto)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status is ShipmentStatus.PickedUp or
                ShipmentStatus.Closed or ShipmentStatus.Cancelled)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"Cannot assign driver to a shipment with status '{shipment.Status}'.");

            var driver = await _userManager.FindByIdAsync(dto.DriverUserId);
            if (driver is null || driver.Role != "Driver")
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Assigned user is not a valid Driver.");

            shipment.DriverUserId = dto.DriverUserId;
            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(id), "Driver assigned successfully.");
        }

        // ── Mark In-Transit ───────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> MarkInTransitAsync(
            int id, string updatedByUserId)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status != ShipmentStatus.Pending)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"Only Pending shipments can be marked In-Transit. Current status: {shipment.Status}.");

            shipment.Status = ShipmentStatus.InTransit;
            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(id), "Shipment marked as In-Transit.");
        }

        // ── Record Arrival ────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> RecordArrivalAsync(
            int id, RecordArrivalDto dto, string updatedByUserId)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status is ShipmentStatus.ArrivedAtDestination or
                ShipmentStatus.DemurrageActive or ShipmentStatus.PickedUp or
                ShipmentStatus.Closed or ShipmentStatus.Cancelled)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    $"Arrival already recorded or shipment is in a terminal state ({shipment.Status}).");

            // WAT = UTC+1; use today if no date supplied
            var watNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                   TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time"));
            var arrivalDate = dto.ArrivalDate?.Date ?? watNow.Date;

            if (arrivalDate > watNow.Date)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Arrival date cannot be in the future.");

            shipment.ActualArrivalDate = arrivalDate;
            shipment.FreeDaysExpiry = arrivalDate.AddDays(FREE_DAYS);
            shipment.Status = ShipmentStatus.ArrivedAtDestination;

            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(id),
                $"Arrival recorded. Free days expire on {shipment.FreeDaysExpiry:dd MMM yyyy}.");
        }

        // ── Record Pickup ─────────────────────────────────────────────────
        public async Task<ApiResponse<ShipmentResponseDto>> RecordPickupAsync(
            int id, RecordPickupDto dto, string updatedByUserId)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status is ShipmentStatus.Pending or
                ShipmentStatus.InTransit or ShipmentStatus.Cancelled)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Item must have arrived before it can be picked up.");

            if (shipment.Status is ShipmentStatus.PickedUp or ShipmentStatus.Closed)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment already picked up.");

            var watNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                  TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time"));
            var pickupDate = dto.PickupDate?.Date ?? watNow.Date;

            shipment.PickedUpAt = pickupDate;
            shipment.Status = ShipmentStatus.PickedUp;
            shipment.IsDemurrageActive = false;

            // Settle all unsettled accruals for this shipment
            var unsettled = await _db.DemurrageAccruals
                .Where(a => a.ShipmentId == id && !a.IsSettled)
                .ToListAsync();

            foreach (var accrual in unsettled)
                accrual.IsSettled = true;

            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseDto(id), "Pickup recorded. Demurrage accrual stopped.");
        }

        // ── Cancel ────────────────────────────────────────────────────────
        public async Task<ApiResponse<bool>> CancelAsync(int id, string cancelledByUserId)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment is null)
                return ApiResponse<bool>.Fail("Shipment not found.");

            if (shipment.Status is ShipmentStatus.PickedUp or
                ShipmentStatus.Closed or ShipmentStatus.Cancelled)
                return ApiResponse<bool>.Fail(
                    $"Cannot cancel a shipment with status '{shipment.Status}'.");

            if (shipment.IsDemurrageActive)
                return ApiResponse<bool>.Fail(
                    "Cannot cancel a shipment with active demurrage. Settle the invoice first.");

            shipment.Status = ShipmentStatus.Cancelled;
            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Shipment cancelled successfully.");
        }

        // ── Private: Build full response DTO ─────────────────────────────
        private async Task<ShipmentResponseDto> BuildResponseDto(int shipmentId)
        {
            var s = await _db.Shipments
                .Include(s => s.Customer).ThenInclude(c => c.User)
                .Include(s => s.Driver)
                .FirstAsync(s => s.Id == shipmentId);

            var accrued = await _db.DemurrageAccruals
                .Where(a => a.ShipmentId == shipmentId && !a.IsSettled)
                .OrderByDescending(a => a.AccrualDate)
                .FirstOrDefaultAsync();

            // Days in storage (from arrival to today or pickup)
            int? daysInStorage = null;
            int? demurrageDays = null;

            if (s.ActualArrivalDate.HasValue)
            {
                var watNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                      TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time"));
                var endDate = s.PickedUpAt?.Date ?? watNow.Date;
                daysInStorage = (endDate - s.ActualArrivalDate.Value.Date).Days;
                demurrageDays = Math.Max(0, daysInStorage.Value - FREE_DAYS);
            }

            return new ShipmentResponseDto
            {
                Id = s.Id,
                WaybillNumber = s.WaybillNumber,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.User.FullName,
                CustomerCompany = s.Customer.CompanyName,
                CustomerPhone = s.Customer.PhoneNumber,
                DriverUserId = s.DriverUserId,
                DriverName = s.Driver?.FullName,
                OriginState = s.OriginState,
                DestinationState = s.DestinationState,
                ItemDescription = s.ItemDescription,
                DeclaredValue = s.DeclaredValue,
                Weight = s.Weight,
                Status = s.Status.ToString(),
                CreatedAt = s.CreatedAt,
                ExpectedArrivalDate = s.ExpectedArrivalDate,
                ActualArrivalDate = s.ActualArrivalDate,
                FreeDaysExpiry = s.FreeDaysExpiry,
                PickedUpAt = s.PickedUpAt,
                IsDemurrageActive = s.IsDemurrageActive,
                DaysInStorage = daysInStorage,
                DemurrageDays = demurrageDays,
                AccruedDemurrageAmount = accrued?.AccumulatedTotal,
                CreatedByUserId = s.CreatedByUserId
            };
        }
    }
}