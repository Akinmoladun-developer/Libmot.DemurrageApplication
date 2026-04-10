using Libmot.DemurrageApplicationAPI.DTOs.Auth;
using Libmot.DemurrageApplicationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libmot.DemurrageApplicationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.RegisterAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Merchant/Customer can login and receive JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(dto);
            return response.Success ? Ok(response) : Unauthorized(response);
        }

        // Authenticating and merchant/customer change of password
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.ChangePasswordAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Authenticating and updating  profile
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _authService.UpdateProfileAsync(userId, dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // Access for SuperAdmin only
        [HttpGet("users")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _authService.GetAllUsersAsync();
            return Ok(response);
        }

        // Deactivating merchant's/customer account for violating Terms of Service. Access available to SuperAdmin only
        [HttpPatch("deactivate/{userId}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var response = await _authService.DeactivateUserAsync(userId);
            return response.Success ? Ok(response) : NotFound(response);
        }
    }
}

