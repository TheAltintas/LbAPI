using Microsoft.AspNetCore.Mvc;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;
using LittleBeaconAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, AppDbContext context)
        {
            _authService = authService;
            _logger = logger;
            _context = context;
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

        [HttpPost("register")]
        public ActionResult<LoginResponse> Register([FromBody] LoginRequest request)
        {
            _logger.LogInformation($"Register attempt for user: {request.Username}");

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Indtast venligst brugernavn og adgangskode"
                });
            }

            var response = _authService.Register(request.Username, request.Password);

            if (!response.Success)
            {
                return Conflict(response);
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            // In a real app, you would invalidate the token here
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/admin")]
        public async Task<ActionResult> GetAllUsersAdmin([FromHeader(Name = "Authorization")] string? authHeader)
        {
            if (!IsAdmin(authHeader))
            {
                return Unauthorized(new { success = false, message = "Admin access required" });
            }

            var users = await _context.Users
                .Select(u => new { u.Id, u.Username, u.Role, u.CreatedAt })
                .ToListAsync();

            return Ok(new { success = true, data = users, message = "Users retrieved successfully" });
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult> GetUserById(int id, [FromHeader(Name = "Authorization")] string? authHeader)
        {
            if (!IsAdmin(authHeader))
            {
                return Unauthorized(new { success = false, message = "Admin access required" });
            }

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new { u.Id, u.Username, u.Role, u.CreatedAt })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new { success = true, data = user, message = "User retrieved successfully" });
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUser(int id, [FromHeader(Name = "Authorization")] string? authHeader)
        {
            if (!IsAdmin(authHeader))
            {
                return Unauthorized(new { success = false, message = "Admin access required" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Prevent deleting admin user
            if (user.Username.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Cannot delete admin user" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User deleted successfully" });
        }

        private bool IsAdmin(string? authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return false;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var username = _authService.GetUsernameFromToken(token);

            return username != null && username.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
