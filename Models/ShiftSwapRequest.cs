using System.Text.Json.Serialization;

namespace LittleBeaconAPI.Models
{
    public enum ShiftSwapStatus
    {
        Pending,
        Approved,
        Declined,
        Cancelled
    }

    public class ShiftSwapRequest
    {
        public int Id { get; set; }
        public int RequestedByUserId { get; set; }
        public int RequestedByShiftId { get; set; }
        public int RequestedWithUserId { get; set; }
        public int? RequestedWithShiftId { get; set; }
        public string? Message { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftSwapStatus Status { get; set; } = ShiftSwapStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
        public string? ResolutionNote { get; set; }
        public bool IsHandover { get; set; } = false;

        [JsonIgnore]
        public bool IsClosed => Status == ShiftSwapStatus.Approved || Status == ShiftSwapStatus.Declined || Status == ShiftSwapStatus.Cancelled;
    }

    public class ShiftSwapRequestSummary
    {
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShiftSwapStatus Status { get; set; }
        public int RequestedByUserId { get; set; }
        public int RequestedByShiftId { get; set; }
        public int RequestedWithUserId { get; set; }
        public int? RequestedWithShiftId { get; set; }
        public string? Message { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
        public string? ResolutionNote { get; set; }
        public bool IsHandover { get; set; } = false;
        public ShiftSummary? RequestedByShift { get; set; }
        public ShiftSummary? RequestedWithShift { get; set; }
    }

    public class ShiftSummary
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public int UserId { get; set; }
        public string? Status { get; set; }
    }
}
