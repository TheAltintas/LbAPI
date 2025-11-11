namespace LittleBeaconAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        public ICollection<SickReport> SickReports { get; set; } = new List<SickReport>();
        public ICollection<Note> NoteEntries { get; set; } = new List<Note>();
    }
}
