using LittleBeaconAPI.Data;
using LittleBeaconAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LittleBeaconAPI.Services
{
    public interface IShiftService
    {
        List<Shift> GetUpcomingShifts(int userId);
        List<Shift> GetAllShifts();
        List<Shift> GetShiftsByUserId(int userId);
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
            RepairInvalidDates();
            return _context.Shifts
                .Where(s => s.UserId == userId && !s.IsCompleted)
                .OrderBy(s => s.ActualDate)
                .AsNoTracking()
                .ToList();
        }

        public List<Shift> GetAllShifts()
        {
            RepairInvalidDates();
            return _context.Shifts
                .OrderBy(s => s.ActualDate)
                .AsNoTracking()
                .ToList();
        }

        public List<Shift> GetShiftsByUserId(int userId)
        {
            RepairInvalidDates();
            return _context.Shifts
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.ActualDate)
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
            NormalizeShiftDates(shift);

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
                existing.ActualDate = shift.ActualDate;
                existing.Time = shift.Time;
                existing.Location = shift.Location;
                existing.Tag = shift.Tag;
                existing.UserId = shift.UserId;
                existing.Status = string.IsNullOrWhiteSpace(shift.Status) ? existing.Status : shift.Status;
                existing.IsCompleted = shift.IsCompleted;
                existing.Hours = shift.Hours;
                existing.Notes = shift.Notes;
                existing.BorderColor = shift.BorderColor;
                existing.WeekOffset = shift.WeekOffset;
                existing.SickReportId = shift.SickReportId;

                NormalizeShiftDates(existing);

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

        private static DateTime NormalizeToDateOnlyUtc(DateTime value, string? fallbackDate)
        {
            // If the value is unset or a sentinel year, try fallback string
            if (value == default || value.Year < 2000)
            {
                if (!string.IsNullOrWhiteSpace(fallbackDate) && DateTime.TryParse(fallbackDate, out var parsed))
                {
                    value = parsed;
                }
            }

            // Keep only the date component and mark as UTC to avoid timezone shifts
            var dateOnly = value.Date;
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
        }

        private static string ToIsoDateString(DateTime value)
        {
            return value.ToString("yyyy-MM-dd");
        }

        private static void NormalizeShiftDates(Shift shift)
        {
            var normalizedDate = NormalizeToDateOnlyUtc(shift.ActualDate, shift.Date);
            shift.ActualDate = normalizedDate;
            shift.Date = ToIsoDateString(normalizedDate);
        }

        private void RepairInvalidDates()
        {
            var candidates = _context.Shifts
                .Where(s => s.ActualDate == default || s.ActualDate.Year < 2000 || string.IsNullOrWhiteSpace(s.Date))
                .ToList();

            if (!candidates.Any())
            {
                return;
            }

            var changed = false;
            foreach (var shift in candidates)
            {
                var beforeDate = shift.Date;
                var beforeActual = shift.ActualDate;
                NormalizeShiftDates(shift);
                if (!Equals(beforeDate, shift.Date) || !Equals(beforeActual, shift.ActualDate))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                _context.SaveChanges();
            }
        }
    }
}
