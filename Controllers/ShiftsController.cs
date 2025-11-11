using Microsoft.AspNetCore.Mvc;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;

namespace LittleBeaconAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _shiftService;
        private readonly IAuthService _authService;
        private readonly ILogger<ShiftsController> _logger;

        public ShiftsController(
            IShiftService shiftService, 
            IAuthService authService,
            ILogger<ShiftsController> logger)
        {
            _shiftService = shiftService;
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<Shift>> GetAllShifts()
        {
            var shifts = _shiftService.GetAllShifts();
            return Ok(shifts);
        }

        [HttpGet("{id}")]
        public ActionResult<Shift> GetShift(int id)
        {
            var shift = _shiftService.GetShiftById(id);
            if (shift == null)
            {
                return NotFound();
            }
            return Ok(shift);
        }

        [HttpGet("upcoming/{userId}")]
        public ActionResult<List<Shift>> GetUpcomingShifts(int userId)
        {
            var shifts = _shiftService.GetUpcomingShifts(userId);
            return Ok(shifts);
        }

        [HttpPost]
        public ActionResult<Shift> CreateShift([FromBody] Shift shift)
        {
            _shiftService.AddShift(shift);
            return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, shift);
        }

        [HttpPut("{id}")]
        public ActionResult UpdateShift(int id, [FromBody] Shift shift)
        {
            if (id != shift.Id)
            {
                return BadRequest();
            }

            _shiftService.UpdateShift(shift);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteShift(int id)
        {
            _shiftService.DeleteShift(id);
            return NoContent();
        }
    }
}
