using Microsoft.AspNetCore.Mvc;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;

namespace LittleBeaconAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation($"Login attempt for user: {request.Username}");

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Indtast venligst brugernavn og adgangskode"
                });
            }

            var response = _authService.Authenticate(request.Username, request.Password);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("validate")]
        public ActionResult<bool> ValidateToken([FromBody] string token)
        {
            var isValid = _authService.ValidateToken(token);
            return Ok(isValid);
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            // In a real app, you would invalidate the token here
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
