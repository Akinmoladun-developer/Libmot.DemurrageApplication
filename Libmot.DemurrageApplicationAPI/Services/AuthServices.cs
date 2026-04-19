using Libmot.DemurrageApplication.Data;
using Libmot.DemurrageApplication.Models;
using Libmot.DemurrageApplicationAPI.DTOs.Auth;
using Libmot.DemurrageApplicationAPI.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Libmot.DemurrageApplicationAPI.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto, string createdByUserId);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId);
        Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ApiResponse<bool>> ToggleUserActiveAsync(string targetUserId, string adminUserId);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;
        private readonly IJwtService _jwtService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            AppDbContext db,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _db = db;
            _jwtService = jwtService;
        }

        // Register Merchant
        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(
            RegisterRequestDto dto, string createdByUserId)
        {
            var allowedRoles = new[] { "SuperAdmin", "FinanceOfficer", "Customer", "Driver" };
            if (!allowedRoles.Contains(dto.Role))
                return ApiResponse<AuthResponseDto>.Fail($"Role '{dto.Role}' is not valid.");

            if (await _userManager.FindByEmailAsync(dto.Email) is not null)
                return ApiResponse<AuthResponseDto>.Fail("A user with this email already exists.");

            if (dto.Role == "Customer" &&
                (string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.State)))
                return ApiResponse<AuthResponseDto>.Fail(
                    "CompanyName and State are required when registering a Customer.");

            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                UserName = dto.Email.Trim().ToLower(),
                Email = dto.Email.Trim().ToLower(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Role = dto.Role,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDto>.Fail("User creation failed.", errors);
            }

            await _userManager.AddToRoleAsync(user, dto.Role);

            // Create customer profile record if role is Customer
            if (dto.Role == "Customer")
            {
                var customer = new Customer
                {
                    UserId = user.Id,
                    CompanyName = dto.CompanyName!.Trim(),
                    ContactPerson = dto.ContactPerson?.Trim() ?? dto.FullName.Trim(),
                    Address = dto.Address?.Trim() ?? string.Empty,
                    State = dto.State!.Trim(),
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    Email = dto.Email.Trim().ToLower()
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiry) = _jwtService.GenerateToken(user, roles);

            return ApiResponse<AuthResponseDto>.Ok(
                BuildAuthResponse(user, token, expiry, null),
                "User registered successfully.");
        }

        // Login-In User
        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

            if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Fail(
                    "Your account has been deactivated. Contact support.");

            //var customer = user.Role == "Customer"
               // ? await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id)
               // : null;

            var customer = user.Role == "Customer"
            ? await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id)
            : null;

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiry) = _jwtService.GenerateToken(user, roles);

            return ApiResponse<AuthResponseDto>.Ok(
                BuildAuthResponse(user, token, expiry, customer),
                "Login successful.");
        }

        // Getting Merchant's profile 
        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<UserProfileDto>.Fail("User not found.");

            var customer = user.Role == "Customer"
                ? await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId)
                : null;

            return ApiResponse<UserProfileDto>.Ok(MapToProfileDto(user, customer));
        }

        // Update Merchant's profile
        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(
            string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<UserProfileDto>.Fail("User not found.");

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<UserProfileDto>.Fail("Profile update failed.", errors);
            }

            Customer? customer = null;
            if (user.Role == "Customer")
            {
                customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer is not null)
                {
                    customer.CompanyName = dto.CompanyName?.Trim() ?? customer.CompanyName;
                    customer.ContactPerson = dto.ContactPerson?.Trim() ?? customer.ContactPerson;
                    customer.Address = dto.Address?.Trim() ?? customer.Address;
                    customer.State = dto.State?.Trim() ?? customer.State;
                    customer.PhoneNumber = dto.PhoneNumber.Trim();
                    await _db.SaveChangesAsync();
                }
            }

            return ApiResponse<UserProfileDto>.Ok(
                MapToProfileDto(user, customer), "Profile updated successfully.");
        }

        // Merchant's Change password 
        public async Task<ApiResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return ApiResponse<bool>.Fail("User not found.");

            var result = await _userManager.ChangePasswordAsync(
                user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<bool>.Fail("Password change failed.", errors);
            }

            return ApiResponse<bool>.Ok(true, "Password changed successfully.");
        }

        // Toggle active. This is for SuperAdmin only
        public async Task<ApiResponse<bool>> ToggleUserActiveAsync(
            string targetUserId, string adminUserId)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user is null)
                return ApiResponse<bool>.Fail("User not found.");

            if (user.Id == adminUserId)
                return ApiResponse<bool>.Fail("You cannot deactivate your own account.");

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var state = user.IsActive ? "activated" : "deactivated";
            return ApiResponse<bool>.Ok(true, $"User account has been {state}.");
        }

        // Private helpers method 
        private static AuthResponseDto BuildAuthResponse(
            ApplicationUser user, string token, DateTime expiry, Customer? customer) => new()
            {
                Token = token,
                ExpiresAt = expiry,
                User = MapToProfileDto(user, customer)
            };


        private static UserProfileDto MapToProfileDto(ApplicationUser user, Customer? customer) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CustomerProfile = customer is null ? null : new CustomerProfileDto
            {
                Id = customer.Id,
                CompanyName = customer.CompanyName,
                ContactPerson = customer.ContactPerson,
                Address = customer.Address,
                State = customer.State
            }
        };
    }
}
