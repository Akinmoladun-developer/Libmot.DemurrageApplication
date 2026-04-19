using Libmot.DemurrageApplicationAPI.DTOs.Auth;
using Libmot.DemurrageApplicationAPI.Helpers;
using Libmot.DemurrageApplicationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libmot.DemurrageApplicationAPI.Controllers
{
     
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new user. SuperAdmin only.
        /// Roles: SuperAdmin | FinanceOfficer | Customer | Driver
        /// </summary>
        [HttpPost("register")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.RegisterAsync(dto, adminId);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Login and receive a JWT token.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(dto);
            return response.Success ? Ok(response) : Unauthorized(response);
        }

        /// <summary>
        /// Get the currently authenticated user's profile.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.GetProfileAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        /// <summary>
        /// Update the currently authenticated user's profile.
        /// </summary>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), 400)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.UpdateProfileAsync(userId, dto);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Change the currently authenticated user's password.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.ChangePasswordAsync(userId, dto);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Activate or deactivate a user account. SuperAdmin only.
        /// </summary>
        [HttpPatch("users/{userId}/toggle-active")]
        [Authorize(Policy = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        public async Task<IActionResult> ToggleActive(string userId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.ToggleUserActiveAsync(userId, adminId);

            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
