namespace LittleBeaconAPI.Models
{
    public class Note
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int? UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Shift? Shift { get; set; }
        public User? User { get; set; }
    }
}
