using System.Collections.Generic;
using System.Linq;
using LittleBeaconAPI.Data;
using LittleBeaconAPI.Models;
using LittleBeaconAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftSwapsController : ControllerBase
    {
        private readonly IShiftSwapService _swapService;
        private readonly ILogger<ShiftSwapsController> _logger;
        private readonly AppDbContext _db;

        public ShiftSwapsController(
            IShiftSwapService swapService,
            ILogger<ShiftSwapsController> logger,
            AppDbContext db)
        {
            _swapService = swapService;
            _logger = logger;
            _db = db;
        }

        public class CreateSwapRequestDto
        {
            public int RequestedByUserId { get; set; }
            public int RequestedByShiftId { get; set; }
            public int RequestedWithUserId { get; set; }
            public int? RequestedWithShiftId { get; set; }
            public string? Message { get; set; }
            public bool IsHandover { get; set; } = false;
        }

        public class DecideSwapRequestDto
        {
            public string Decision { get; set; } = string.Empty;
            public int ResolverUserId { get; set; }
            public string? Note { get; set; }
        }

        public class CancelSwapRequestDto
        {
            public int UserId { get; set; }
            public string? Reason { get; set; }
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult> GetForUser(int userId)
        {
            var requests = _swapService.GetForUser(userId);
            var payload = await BuildSummariesAsync(requests);
            return Ok(new { success = true, data = payload, message = "Requests retrieved" });
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var requests = _swapService.GetAll();
            var payload = await BuildSummariesAsync(requests);
            return Ok(new { success = true, data = payload, message = "All requests retrieved" });
        }

        [HttpGet("pending")]
        public async Task<ActionResult> GetPending()
        {
            var requests = _swapService.GetPending();
            var payload = await BuildSummariesAsync(requests);
            return Ok(new { success = true, data = payload, message = "Pending requests retrieved" });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
        {
            var request = _swapService.GetById(id);
            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            var payload = await BuildSummariesAsync(new[] { request });
            return Ok(new { success = true, data = payload.FirstOrDefault(), message = "Request retrieved" });
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateSwapRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.RequestedByUserId <= 0 || dto.RequestedWithUserId <= 0)
            {
                return BadRequest(new { success = false, message = "User IDs must be greater than zero" });
            }

            if (dto.RequestedByShiftId <= 0)
            {
                return BadRequest(new { success = false, message = "Requester shift id is required" });
            }

            var fromShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.RequestedByShiftId);
            if (fromShift == null)
            {
                return BadRequest(new { success = false, message = "Shift for requesting user was not found" });
            }

            if (fromShift.UserId != dto.RequestedByUserId)
            {
                return BadRequest(new { success = false, message = "Shift does not belong to requesting user" });
            }

            var targetUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.RequestedWithUserId);
            if (targetUser == null)
            {
                return BadRequest(new { success = false, message = "Target user was not found" });
            }

            if (dto.RequestedWithUserId == dto.RequestedByUserId)
            {
                return BadRequest(new { success = false, message = "Du kan ikke bytte eller overdrage til dig selv" });
            }

            if (dto.IsHandover)
            {
                var existingHandover = _swapService
                    .GetAll()
                    .Any(r => r.Status == ShiftSwapStatus.Pending && r.RequestedByShiftId == dto.RequestedByShiftId);

                if (existingHandover)
                {
                    return Conflict(new { success = false, message = "Der findes allerede en afventende anmodning for denne vagt" });
                }
            }
            else
            {
                if (dto.RequestedWithShiftId == null || dto.RequestedWithShiftId <= 0)
                {
                    return BadRequest(new { success = false, message = "Target shift id is required for bytte" });
                }

                if (dto.RequestedByShiftId == dto.RequestedWithShiftId)
                {
                    return BadRequest(new { success = false, message = "Cannot swap the same shift" });
                }

                var targetShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.RequestedWithShiftId);
                if (targetShift == null)
                {
                    return BadRequest(new { success = false, message = "Target shift was not found" });
                }

                if (targetShift.UserId != dto.RequestedWithUserId)
                {
                    return BadRequest(new { success = false, message = "Target shift does not belong to selected colleague" });
                }

                var existingPending = _swapService
                    .GetAll()
                    .Any(r => r.Status == ShiftSwapStatus.Pending &&
                              !r.IsHandover &&
                              ((r.RequestedByShiftId == dto.RequestedByShiftId && r.RequestedWithShiftId == dto.RequestedWithShiftId) ||
                               (r.RequestedByShiftId == dto.RequestedWithShiftId && r.RequestedWithShiftId == dto.RequestedByShiftId)));

                if (existingPending)
                {
                    return Conflict(new { success = false, message = "Der er allerede en afventende bytteanmodning for disse vagter" });
                }
            }

            var request = new ShiftSwapRequest
            {
                RequestedByUserId = dto.RequestedByUserId,
                RequestedByShiftId = dto.RequestedByShiftId,
                RequestedWithUserId = dto.RequestedWithUserId,
                RequestedWithShiftId = dto.RequestedWithShiftId,
                Message = dto.Message,
                IsHandover = dto.IsHandover
            };

            var created = _swapService.Create(request);
            var payload = await BuildSummariesAsync(new[] { created });

            return Ok(new { success = true, data = payload.First(), message = "Swap request created" });
        }

        [HttpPost("{id:int}/decision")]
        public async Task<ActionResult> Decide(int id, [FromBody] DecideSwapRequestDto dto)
        {
            if (dto.ResolverUserId <= 0)
            {
                return BadRequest(new { success = false, message = "Resolver user id must be provided" });
            }

            var desiredStatus = ParseDecision(dto.Decision);
            if (desiredStatus == null)
            {
                return BadRequest(new { success = false, message = "Decision must be approve or decline" });
            }

            var updated = _swapService.UpdateStatus(id, desiredStatus.Value, dto.ResolverUserId, dto.Note);
            if (updated == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            // When a swap is approved, swap the shift owners in the database so the change is visible under "Vis vagtplan".
            if (updated.Status == ShiftSwapStatus.Approved)
            {
                var applied = updated.IsHandover
                    ? await TransferShiftOwnerAsync(updated.RequestedByShiftId, updated.RequestedWithUserId)
                    : await SwapShiftOwnersAsync(updated.RequestedByShiftId, updated.RequestedWithShiftId ?? 0);

                if (!applied)
                {
                    return StatusCode(500, new { success = false, message = "Kunne ikke opdatere vagterne" });
                }
            }

            var payload = await BuildSummariesAsync(new[] { updated });
            return Ok(new { success = true, data = payload.First(), message = "Swap request updated" });
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult> Cancel(int id, [FromBody] CancelSwapRequestDto dto)
        {
            if (dto.UserId <= 0)
            {
                return BadRequest(new { success = false, message = "User id must be provided" });
            }

            var updated = _swapService.Cancel(id, dto.UserId, dto.Reason);
            if (updated == null)
            {
                return NotFound(new { success = false, message = "Request not found" });
            }

            var payload = await BuildSummariesAsync(new[] { updated });
            return Ok(new { success = true, data = payload.First(), message = "Swap request cancelled" });
        }

        private ShiftSwapStatus? ParseDecision(string decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
            {
                return null;
            }

            var normalized = decision.Trim().ToLowerInvariant();
            return normalized switch
            {
                "approve" => ShiftSwapStatus.Approved,
                "approved" => ShiftSwapStatus.Approved,
                "accept" => ShiftSwapStatus.Approved,
                "decline" => ShiftSwapStatus.Declined,
                "declined" => ShiftSwapStatus.Declined,
                "reject" => ShiftSwapStatus.Declined,
                _ => null
            };
        }

        private async Task<List<ShiftSwapRequestSummary>> BuildSummariesAsync(IEnumerable<ShiftSwapRequest> requests)
        {
            var list = requests.ToList();
            if (list.Count == 0)
            {
                return new List<ShiftSwapRequestSummary>();
            }

            var shiftIds = list
                .SelectMany(r => new[] { r.RequestedByShiftId, r.RequestedWithShiftId })
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var shiftLookup = await _db.Shifts
                .Where(s => shiftIds.Contains(s.Id))
                .Select(s => new ShiftSummary
                {
                    Id = s.Id,
                    Date = s.Date,
                    Time = s.Time,
                    Location = s.Location,
                    Tag = s.Tag,
                    UserId = s.UserId,
                    Status = s.Status
                })
                .ToDictionaryAsync(s => s.Id);

            return list.Select(r => new ShiftSwapRequestSummary
            {
                Id = r.Id,
                Status = r.Status,
                RequestedByUserId = r.RequestedByUserId,
                RequestedByShiftId = r.RequestedByShiftId,
                RequestedWithUserId = r.RequestedWithUserId,
                RequestedWithShiftId = r.RequestedWithShiftId,
                Message = r.Message,
                RequestedAt = r.RequestedAt,
                ResolvedAt = r.ResolvedAt,
                ResolvedByUserId = r.ResolvedByUserId,
                ResolutionNote = r.ResolutionNote,
                IsHandover = r.IsHandover,
                RequestedByShift = shiftLookup.TryGetValue(r.RequestedByShiftId, out var fromShift) ? fromShift : null,
                RequestedWithShift = r.RequestedWithShiftId.HasValue && shiftLookup.TryGetValue(r.RequestedWithShiftId.Value, out var toShift) ? toShift : null
            }).ToList();
        }

        private async Task<bool> SwapShiftOwnersAsync(int shiftIdA, int shiftIdB)
        {
            var shiftIds = new[] { shiftIdA, shiftIdB };
            var shifts = await _db.Shifts.Where(s => shiftIds.Contains(s.Id)).ToListAsync();
            if (shifts.Count != 2)
            {
                return false;
            }

            var first = shifts.First(s => s.Id == shiftIdA);
            var second = shifts.First(s => s.Id == shiftIdB);

            var tempUserId = first.UserId;
            first.UserId = second.UserId;
            second.UserId = tempUserId;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<bool> TransferShiftOwnerAsync(int shiftId, int targetUserId)
        {
            var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId);
            if (shift == null)
            {
                return false;
            }

            var targetUserExists = await _db.Users.AnyAsync(u => u.Id == targetUserId);
            if (!targetUserExists)
            {
                return false;
            }

            shift.UserId = targetUserId;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
