using Libmot.DemurrageApplicationAPI.DTOs.Auth;
using Libmot.DemurrageApplicationAPI.DTOs.Common;
using Libmot.DemurrageApplicationAPI.Helpers;
using Libmot.DemurrageApplicationAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        // Roles that only a SuperAdmin can assign
        private static readonly string[] ProtectedRoles =
            ["SuperAdmin", "FinanceBillingOfficer", "DriverDispatch"];

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            // Validating role exists
            if (!await _roleManager.RoleExistsAsync(dto.Role))
                return ApiResponse<AuthResponseDto>.Fail($"Role '{dto.Role}' does not exist.");

            // Prevent self-registration into protected roles
            if (ProtectedRoles.Contains(dto.Role))
                return ApiResponse<AuthResponseDto>.Fail(
                    "You cannot self-register with this role. Contact an administrator.");

            // Checking if duplicate customer/merchants email exist
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return ApiResponse<AuthResponseDto>.Fail("This email already exists.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                CompanyName = dto.CompanyName,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return ApiResponse<AuthResponseDto>.Fail(
                    "Registration failed.",
                    result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, dto.Role);

            var (token, expiresAt) = JwtHelper.GenerateToken(user, dto.Role, _config);

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Role = dto.Role,
                ExpiresAt = expiresAt
            }, "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials or account is inactive.");

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
                return ApiResponse<AuthResponseDto>.Fail("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";

            var (token, expiresAt) = JwtHelper.GenerateToken(user, role, _config);

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Role = role,
                ExpiresAt = expiresAt
            }, "Login successful.");
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            var result = await _userManager.ChangePasswordAsync(
                user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return ApiResponse<string>.Fail(
                    "Password change failed.",
                    result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<string>.Ok("Password changed successfully.");
        }

        public async Task<ApiResponse<string>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            user.FullName = dto.FullName;
            user.CompanyName = dto.CompanyName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponse<string>.Fail(
                    "Profile update failed.",
                    result.Errors.Select(e => e.Description).ToList());

            return ApiResponse<string>.Ok("Profile updated successfully.");
        }

        public async Task<ApiResponse<string>> DeactivateUserAsync(string targetUserId)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Ok($"User '{user.Email}' has been deactivated.");
        }

        public async Task<ApiResponse<List<UserListDto>>> GetAllUsersAsync()
        {
            var users = _userManager.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            var result = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserListDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    CompanyName = user.CompanyName,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "N/A",
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            return ApiResponse<List<UserListDto>>.Ok(result);
        }
    }
}
