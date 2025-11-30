namespace LittleBeaconAPI.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public DateTime ActualDate { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public int UserId { get; set; }
        public bool IsCompleted { get; set; }
        public int Hours { get; set; } = 8;
        public string? Status { get; set; } = "Vagt";
        public string? Notes { get; set; }
        public string? BorderColor { get; set; }
        public int WeekOffset { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? SickReportId { get; set; }

        public User? User { get; set; }
        public SickReport? SickReport { get; set; }
        public ICollection<Note> NoteEntries { get; set; } = new List<Note>();
    }
}
