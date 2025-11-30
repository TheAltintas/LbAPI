using Microsoft.AspNetCore.Mvc;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;
using LittleBeaconAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;
        private readonly IAuthService _authService;
        private readonly ILogger<ShiftsController> _logger;
        private readonly AppDbContext _db;

        public ShiftsController(
            IShiftService shiftService, 
            IAuthService authService,
            ILogger<ShiftsController> logger,
            AppDbContext db)
        {
            _shiftService = shiftService;
            _authService = authService;
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        public ActionResult GetAllShifts()
        {
            var shifts = _shiftService.GetAllShifts();
            return Ok(new { success = true, data = shifts, message = "Shifts retrieved successfully" });
        }

        [HttpGet("{id}")]
        public ActionResult GetShift(int id)
        {
            var shift = _shiftService.GetShiftById(id);
            if (shift == null)
            {
                return NotFound(new { success = false, message = "Shift not found" });
            }
            return Ok(new { success = true, data = shift, message = "Shift retrieved successfully" });
        }

        [HttpGet("user/{userId}")]
        public ActionResult GetShiftsByUser(int userId)
        {
            var shifts = _shiftService.GetShiftsByUserId(userId);
            return Ok(new { success = true, data = shifts, message = "User shifts retrieved successfully" });
        }

        [HttpGet("upcoming/{userId}")]
        public ActionResult GetUpcomingShifts(int userId)
        {
            var shifts = _shiftService.GetUpcomingShifts(userId);
            return Ok(new { success = true, data = shifts, message = "Upcoming shifts retrieved successfully" });
        }

        [HttpPost]
        public async Task<ActionResult> CreateShift([FromBody] Shift shift, [FromHeader(Name = "Authorization")] string? authHeader)
        {
            // Optional admin check - comment out for testing
            // if (!IsAdmin(authHeader))
            // {
            //     return Unauthorized(new { success = false, message = "Admin access required" });
            // }

            // Validate model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(shift.Date))
            {
                return BadRequest(new { success = false, message = "Date is required" });
            }

            if (string.IsNullOrWhiteSpace(shift.Time))
            {
                return BadRequest(new { success = false, message = "Time is required" });
            }

            if (string.IsNullOrWhiteSpace(shift.Location))
            {
                return BadRequest(new { success = false, message = "Location is required" });
            }

            if (shift.UserId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid UserId is required" });
            }

            // Verify user exists
            var userExists = await _db.Users.AnyAsync(u => u.Id == shift.UserId);
            if (!userExists)
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

            _shiftService.AddShift(shift);
            return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, new { success = true, data = shift, message = "Shift created successfully" });
        }

        [HttpPut("{id}")]
        public ActionResult UpdateShift(int id, [FromBody] Shift shift)
        {
            if (id != shift.Id)
            {
                return BadRequest(new { success = false, message = "ID mismatch" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(shift.Date))
            {
                return BadRequest(new { success = false, message = "Date is required" });
            }

            if (string.IsNullOrWhiteSpace(shift.Time))
            {
                return BadRequest(new { success = false, message = "Time is required" });
            }

            if (string.IsNullOrWhiteSpace(shift.Location))
            {
                return BadRequest(new { success = false, message = "Location is required" });
            }

            _shiftService.UpdateShift(shift);
            return Ok(new { success = true, message = "Shift updated successfully" });
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteShift(int id, [FromHeader(Name = "Authorization")] string? authHeader)
        {
            // Optional admin check - comment out for testing
            // if (!IsAdmin(authHeader))
            // {
            //     return Unauthorized(new { success = false, message = "Admin access required" });
            // }

            _shiftService.DeleteShift(id);
            return Ok(new { success = true, message = "Shift deleted successfully" });
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
