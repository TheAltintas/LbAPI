using LittleBeaconAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<SickReport> SickReports => Set<SickReport>();
        public DbSet<Note> Notes => Set<Note>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Shifts)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SickReports)
                .WithOne(sr => sr.User)
                .HasForeignKey(sr => sr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.NoteEntries)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SickReport>()
                .HasMany(sr => sr.Shifts)
                .WithOne(s => s.SickReport)
                .HasForeignKey(s => s.SickReportId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .Property(n => n.Content)
                .HasMaxLength(2000);
        }
    }
}
