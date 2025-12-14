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

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            var userExists = await _db.Users.AnyAsync(u => u.Id == shift.UserId);
            if (!userExists)
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

            if (string.IsNullOrWhiteSpace(shift.Status))
            {
                shift.Status = "Vagt";
            }

            NormalizeShiftDates(shift);

            await _db.Shifts.AddAsync(shift);
            await _db.SaveChangesAsync();

            // reload to return the persisted entity (with Id)
            var persisted = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shift.Id);

            return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, new { success = true, data = persisted, message = "Shift created successfully" });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateShift(int id, [FromBody] Shift shift)
        {
            if (id != shift.Id)
            {
                return BadRequest(new { success = false, message = "ID mismatch" });
            }

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

            var existing = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Shift not found" });
            }

            existing.Date = shift.Date;
            existing.ActualDate = shift.ActualDate;
            existing.Time = shift.Time;
            existing.Location = shift.Location;
            existing.Tag = shift.Tag;
            existing.UserId = shift.UserId;
            existing.Status = string.IsNullOrWhiteSpace(shift.Status) ? existing.Status : shift.Status;
            existing.IsCompleted = shift.IsCompleted;
            existing.Hours = shift.Hours;
            existing.Notes = shift.Notes;
            existing.BorderColor = shift.BorderColor;
            existing.WeekOffset = shift.WeekOffset;
            existing.SickReportId = shift.SickReportId;

            NormalizeShiftDates(existing);

            await _db.SaveChangesAsync();
            return Ok(new { success = true, data = existing, message = "Shift updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteShift(int id, [FromHeader(Name = "Authorization")] string? authHeader)
        {
            // Optional admin check - comment out for testing
            // if (!IsAdmin(authHeader))
            // {
            //     return Unauthorized(new { success = false, message = "Admin access required" });
            // }

            var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id);
            if (shift == null)
            {
                return NotFound(new { success = false, message = "Shift not found" });
            }

            _db.Shifts.Remove(shift);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = id }, message = "Shift deleted successfully" });
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

        private static DateTime NormalizeToDateOnlyUtc(DateTime value, string? fallbackDate)
        {
            if (value == default || value.Year < 2000)
            {
                if (!string.IsNullOrWhiteSpace(fallbackDate) && DateTime.TryParse(fallbackDate, out var parsed))
                {
                    value = parsed;
                }
            }

            var dateOnly = value.Date;
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
        }

        private static string ToIsoDateString(DateTime value)
        {
            return value.ToString("yyyy-MM-dd");
        }

        private static void NormalizeShiftDates(Shift shift)
        {
            var normalizedDate = NormalizeToDateOnlyUtc(shift.ActualDate, shift.Date);
            shift.ActualDate = normalizedDate;
            shift.Date = ToIsoDateString(normalizedDate);
        }
    }
}
