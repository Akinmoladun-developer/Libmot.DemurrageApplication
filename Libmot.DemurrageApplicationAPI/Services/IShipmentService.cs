using Libmot.DemurrageApplicationAPI.DTOs.Common;
using Libmot.DemurrageApplicationAPI.DTOs.Shipment;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public interface IShipmentService
    {
        Task<ApiResponse<ShipmentResponseDto>> CreateShipmentAsync(CreateShipmentDto dto);
        Task<ApiResponse<ShipmentResponseDto>> GetShipmentByIdAsync(int id);
        Task<ApiResponse<ShipmentResponseDto>> GetShipmentByCodeAsync(string code);
        Task<ApiResponse<PagedResult<ShipmentResponseDto>>> GetAllShipmentsAsync(ShipmentFilterDto filter);
        Task<ApiResponse<PagedResult<ShipmentResponseDto>>> GetShipmentsByCustomerAsync(string customerId, int page, int pageSize);
        Task<ApiResponse<ShipmentResponseDto>> UpdateShipmentAsync(int id, UpdateShipmentDto dto);
        Task<ApiResponse<string>> UpdateContainerStatusAsync(int containerId, UpdateContainerStatusDto dto);
        Task<ApiResponse<string>> DeleteShipmentAsync(int id);
    }
}
