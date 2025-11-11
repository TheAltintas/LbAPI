namespace LittleBeaconAPI.Models
{
    public class Shift
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Vagt";
        public bool IsCompleted { get; set; }
        public double Hours { get; set; } = 8.0;
        public string? Notes { get; set; }
        public int? SickReportId { get; set; }

        public User? User { get; set; }
        public SickReport? SickReport { get; set; }
        public ICollection<Note> NoteEntries { get; set; } = new List<Note>();
    }
}
