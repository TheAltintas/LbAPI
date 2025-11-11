namespace LittleBeaconAPI.Models
{
    public class SickReport
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }

        public User? User { get; set; }
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }
}
