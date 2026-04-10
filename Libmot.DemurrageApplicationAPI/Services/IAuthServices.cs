using Libmot.DemurrageApplicationAPI.DTOs.Auth;
using Libmot.DemurrageApplicationAPI.DTOs.Common;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ApiResponse<string>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ApiResponse<string>> DeactivateUserAsync(string targetUserId);
        Task<ApiResponse<List<UserListDto>>> GetAllUsersAsync();
    }
}
