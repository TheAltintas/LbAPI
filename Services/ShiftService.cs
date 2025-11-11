using LittleBeaconAPI.Data;
using LittleBeaconAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Services
{
    public interface IShiftService
    {
        List<Shift> GetUpcomingShifts(int userId);
        List<Shift> GetAllShifts();
        Shift? GetShiftById(int id);
        void AddShift(Shift shift);
        void UpdateShift(Shift shift);
        void DeleteShift(int id);
    }

    public class ShiftService : IShiftService
    {
        private readonly AppDbContext _context;

        public ShiftService(AppDbContext context)
        {
            _context = context;
        }

        public List<Shift> GetUpcomingShifts(int userId)
        {
            return _context.Shifts
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.Date)
                .AsNoTracking()
                .ToList();
        }

        public List<Shift> GetAllShifts()
        {
            return _context.Shifts
                .AsNoTracking()
                .ToList();
        }

        public Shift? GetShiftById(int id)
        {
            return _context.Shifts
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == id);
        }

        public void AddShift(Shift shift)
        {
            if (string.IsNullOrWhiteSpace(shift.Status))
            {
                shift.Status = "Vagt";
            }

            _context.Shifts.Add(shift);
            _context.SaveChanges();
        }

        public void UpdateShift(Shift shift)
        {
            var existing = _context.Shifts.FirstOrDefault(s => s.Id == shift.Id);
            if (existing != null)
            {
                existing.Date = shift.Date;
                existing.Time = shift.Time;
                existing.Location = shift.Location;
                existing.Status = string.IsNullOrWhiteSpace(shift.Status) ? existing.Status : shift.Status;
                existing.IsCompleted = shift.IsCompleted;
                existing.Hours = shift.Hours;
                existing.Notes = shift.Notes;
                existing.SickReportId = shift.SickReportId;

                _context.SaveChanges();
            }
        }

        public void DeleteShift(int id)
        {
            var shift = _context.Shifts.FirstOrDefault(s => s.Id == id);
            if (shift == null)
            {
                return;
            }

            _context.Shifts.Remove(shift);
            _context.SaveChanges();
        }
    }
}
