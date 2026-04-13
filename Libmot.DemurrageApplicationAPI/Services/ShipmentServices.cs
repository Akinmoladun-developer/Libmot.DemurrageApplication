using Libmot.DemurrageApplicationAPI.Data;
using Libmot.DemurrageApplicationAPI.DTOs.Common;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;
using Libmot.DemurrageApplicationAPI.Models;
using System.ComponentModel;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly AppDbContext _db;

        public ShipmentService(AppDbContext db)
        {
            _db = db;
        }

        // Creating a merchant/Customer's shipment
        public async Task<ApiResponse<ShipmentResponseDto>> CreateShipmentAsync(CreateShipmentDto dto)
        {
            // Validate customer exists
            var customer = await _db.Users.FindAsync(dto.CustomerId);
            if (customer == null)
                return ApiResponse<ShipmentResponseDto>.Fail("Customer not found.");

            // Validate driver if provided
            if (dto.AssignedDriverId != null)
            {
                var driver = await _db.Users.FindAsync(dto.AssignedDriverId);
                if (driver == null)
                    return ApiResponse<ShipmentResponseDto>.Fail("Assigned driver not found.");
            }

            // Validate no duplicate container numbers
            var containerNumbers = dto.Containers.Select(c => c.ContainerNumber).ToList();
            var duplicateInRequest = containerNumbers
                .GroupBy(x => x)
                .Any(g => g.Count() > 1);

            if (duplicateInRequest)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Duplicate container numbers found in the request.");

            var existingContainers = await _db.Containers
                .Where(c => containerNumbers.Contains(c.ContainerNumber))
                .AnyAsync();

            if (existingContainers)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "One or more container numbers already exist in the system.");

            var shipment = new Shipment
            {
                ShipmentCode = await GenerateShipmentCodeAsync(),
                TrackingNumber = dto.BillOfLadingNumber,
                ArrivalDate = dto.ArrivalDate,
                FreeUntilDate = dto.ArrivalDate.AddDays(dto.FreeDaysAllowed),
                FreeDaysAllowed = dto.FreeDaysAllowed,
                CustomerId = dto.CustomerId,
                AssignedDriverId = dto.AssignedDriverId,
                Status = ShipmentStatus.Pending,
            };

            _db.Shipments.Add(shipment);
            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseAsync(shipment.Id),
                "Shipment created successfully.");
        }

        // Getting Shipment by ID
        public async Task<ApiResponse<ShipmentResponseDto>> GetShipmentByIdAsync(int id)
        {
            var exists = await _db.Shipments.AnyAsync(s => s.Id == id);
            if (!exists)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            return ApiResponse<ShipmentResponseDto>.Ok(await BuildResponseAsync(id));
        }

        // Shipment Code
        public async Task<ApiResponse<ShipmentResponseDto>> GetShipmentByCodeAsync(string code)
        {
            var shipment = await _db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentCode == code.ToUpper());

            if (shipment == null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            return ApiResponse<ShipmentResponseDto>.Ok(await BuildResponseAsync(shipment.Id));
        }

        // Pagination and Filtering Result
        public async Task<ApiResponse<PagedResult<ShipmentResponseDto>>> GetAllShipmentsAsync(
            ShipmentFilterDto filter)
        {
            var query = _db.Shipments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.ShipmentCode.ToLower().Contains(term) ||
                    s.TrackingNumber.ToLower().Contains(term);
               
            }

            if (filter.Status.HasValue)
                query = query.Where(s => s.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.CustomerId))
                query = query.Where(s => s.CustomerId == filter.CustomerId);

            if (filter.ArrivalDateFrom.HasValue)
                query = query.Where(s => s.ArrivalDate >= filter.ArrivalDateFrom.Value);

            if (filter.ArrivalDateTo.HasValue)
                query = query.Where(s => s.ArrivalDate <= filter.ArrivalDateTo.Value);

            if (filter.IsDemurrageActive.HasValue && filter.IsDemurrageActive.Value)
                query = query.Where(s =>
                    s.FreeUntilDate.HasValue &&
                    DateTime.UtcNow > s.FreeUntilDate.Value &&
                    s.Status != ShipmentStatus.Delivered);

            var totalCount = await query.CountAsync();

            var shipments = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => s.Id)
                .ToListAsync();

            var items = new List<ShipmentResponseDto>();
            foreach (var id in shipments)
                items.Add(await BuildResponseAsync(id));

            return ApiResponse<PagedResult<ShipmentResponseDto>>.Ok(new PagedResult<ShipmentResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        // Getting Merchant/Customer
        public async Task<ApiResponse<PagedResult<ShipmentResponseDto>>> GetShipmentsByCustomerAsync(
            string customerId, int page, int pageSize)
        {
            var query = _db.Shipments.Where(s => s.CustomerId == customerId);

            var totalCount = await query.CountAsync();

            var ids = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.Id)
                .ToListAsync();

            var items = new List<ShipmentResponseDto>();
            foreach (var id in ids)
                items.Add(await BuildResponseAsync(id));

            return ApiResponse<PagedResult<ShipmentResponseDto>>.Ok(new PagedResult<ShipmentResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // Updating Merchant's Shipment
        public async Task<ApiResponse<ShipmentResponseDto>> UpdateShipmentAsync(
            int id, UpdateShipmentDto dto)
        {
            var shipment = await _db.Shipments.FindAsync(id);
            if (shipment == null)
                return ApiResponse<ShipmentResponseDto>.Fail("Shipment not found.");

            if (shipment.Status == ShipmentStatus.Delivered)
                return ApiResponse<ShipmentResponseDto>.Fail(
                    "Delivered shipments cannot be modified.");

            shipment.Status = dto.Status;
            shipment.FreeDaysAllowed = dto.FreeDaysAllowed;
            shipment.FreeUntilDate = shipment.ArrivalDate.AddDays(dto.FreeDaysAllowed);
            shipment.ActualReleaseDate = dto.ActualReleaseDate;
            shipment.AssignedDriverId = dto.AssignedDriverId;

            await _db.SaveChangesAsync();

            return ApiResponse<ShipmentResponseDto>.Ok(
                await BuildResponseAsync(id),
                "Shipment updated successfully.");
        }

        // Update Container Status 
        //public async Task<ApiResponse<string>> UpdateContainerStatusAsync(
        //    int containerId, UpdateContainerStatusDto dto)
        //{
        //    var container = await _db.Containers.FindAsync(containerId);
        //    if (container == null)
        //        return ApiResponse<string>.Fail("Container not found.");

        //    container.Status = dto.Status;
        //    await _db.SaveChangesAsync();

        //    return ApiResponse<string>.Ok(
        //        $"Container {container.ContainerNumber} status updated to {dto.Status}.");
        //}

        // Deleting Shipment
        public async Task<ApiResponse<string>> DeleteShipmentAsync(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.Containers)
                .Include(s => s.DemurrageRecords)
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return ApiResponse<string>.Fail("Shipment not found.");

            if (shipment.Invoices.Any())
                return ApiResponse<string>.Fail(
                    "Cannot delete a shipment that has associated invoices.");

            _db.Shipments.Remove(shipment);
            await _db.SaveChangesAsync();

            return ApiResponse<string>.Ok(
                $"Shipment {shipment.ShipmentCode} deleted successfully.");
        }

        // Private Helper Method

        private async Task<string> GenerateShipmentCodeAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _db.Shipments.CountAsync(s =>
                s.CreatedAt.Year == year);

            return $"LMX-{year}-{(count + 1):D5}";
        }

        private async Task<ShipmentResponseDto> BuildResponseAsync(int shipmentId)
        {
            var s = await _db.Shipments
                .Include(x => x.Containers)
                .Include(x => x.Customer)
                .Include(x => x.AssignedDriver)
                .FirstAsync(x => x.Id == shipmentId);

            var today = DateTime.UtcNow.Date;
            var freeUntil = s.FreeUntilDate?.Date ?? s.ArrivalDate.AddDays(s.FreeDaysAllowed).Date;
            var daysOverFree = today > freeUntil
                ? (int)(today - freeUntil).TotalDays
                : 0;
            var isDemurrageActive = daysOverFree > 0 &&
                s.Status != ShipmentStatus.Delivered;

            return new ShipmentResponseDto
            {
                Id = s.Id,
                ShipmentCode = s.ShipmentCode,
                TrackingNumber = s.TrackingNumber,
                ArrivalDate = s.ArrivalDate,
                FreeUntilDate = s.FreeUntilDate,
                ActualReleaseDate = s.ActualReleaseDate,
                FreeDaysAllowed = s.FreeDaysAllowed,
                Status = s.Status,
                DaysOverFree = daysOverFree,
                IsDemurrageActive = isDemurrageActive,
                CreatedAt = s.CreatedAt,
                CustomerId = s.CustomerId,
                CustomerName = s.Customer.FullName,
                CustomerEmail = s.Customer.Email!,
                AssignedDriverId = s.AssignedDriverId,
                AssignedDriverName = s.AssignedDriver?.FullName,
                
            };
        }
    }
}
