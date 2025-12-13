using System.Collections.Generic;
using System.Linq;
using LittleBeaconAPI.Models;

namespace LittleBeaconAPI.Services
{
    public interface IShiftSwapService
    {
        ShiftSwapRequest Create(ShiftSwapRequest request);
        ShiftSwapRequest? GetById(int id);
        IEnumerable<ShiftSwapRequest> GetAll();
        IEnumerable<ShiftSwapRequest> GetForUser(int userId);
        IEnumerable<ShiftSwapRequest> GetPending();
        ShiftSwapRequest? UpdateStatus(int id, ShiftSwapStatus status, int resolverUserId, string? note = null);
        ShiftSwapRequest? Cancel(int id, int userId, string? reason = null);
    }

    public class ShiftSwapService : IShiftSwapService
    {
        private static readonly List<ShiftSwapRequest> Requests = new();
        private static readonly object SyncRoot = new();
        private static int NextId = 1;

        public ShiftSwapRequest Create(ShiftSwapRequest request)
        {
            lock (SyncRoot)
            {
                request.Id = NextId++;
                request.Status = ShiftSwapStatus.Pending;
                request.RequestedAt = DateTime.UtcNow;
                Requests.Add(Clone(request));
                return Clone(request);
            }
        }

        public ShiftSwapRequest? GetById(int id)
        {
            lock (SyncRoot)
            {
                var match = Requests.FirstOrDefault(r => r.Id == id);
                return match != null ? Clone(match) : null;
            }
        }

        public IEnumerable<ShiftSwapRequest> GetAll()
        {
            lock (SyncRoot)
            {
                return Requests.Select(Clone).ToList();
            }
        }

        public IEnumerable<ShiftSwapRequest> GetForUser(int userId)
        {
            lock (SyncRoot)
            {
                return Requests
                    .Where(r => r.RequestedByUserId == userId || r.RequestedWithUserId == userId)
                    .Select(Clone)
                    .ToList();
            }
        }

        public IEnumerable<ShiftSwapRequest> GetPending()
        {
            lock (SyncRoot)
            {
                return Requests
                    .Where(r => r.Status == ShiftSwapStatus.Pending)
                    .Select(Clone)
                    .ToList();
            }
        }

        public ShiftSwapRequest? UpdateStatus(int id, ShiftSwapStatus status, int resolverUserId, string? note = null)
        {
            lock (SyncRoot)
            {
                var request = Requests.FirstOrDefault(r => r.Id == id);
                if (request == null)
                {
                    return null;
                }

                if (request.IsClosed && status == ShiftSwapStatus.Pending)
                {
                    return Clone(request);
                }

                if (request.Status != ShiftSwapStatus.Pending && status != request.Status)
                {
                    return Clone(request);
                }

                request.Status = status;
                request.ResolvedAt = DateTime.UtcNow;
                request.ResolvedByUserId = resolverUserId;
                request.ResolutionNote = note;

                return Clone(request);
            }
        }

        public ShiftSwapRequest? Cancel(int id, int userId, string? reason = null)
        {
            lock (SyncRoot)
            {
                var request = Requests.FirstOrDefault(r => r.Id == id);
                if (request == null)
                {
                    return null;
                }

                if (request.Status != ShiftSwapStatus.Pending)
                {
                    return Clone(request);
                }

                if (request.RequestedByUserId != userId && request.RequestedWithUserId != userId)
                {
                    return Clone(request);
                }

                request.Status = ShiftSwapStatus.Cancelled;
                request.ResolvedAt = DateTime.UtcNow;
                request.ResolutionNote = reason;
                request.ResolvedByUserId = userId;

                return Clone(request);
            }
        }

        private static ShiftSwapRequest Clone(ShiftSwapRequest source)
        {
            return new ShiftSwapRequest
            {
                Id = source.Id,
                RequestedByUserId = source.RequestedByUserId,
                RequestedByShiftId = source.RequestedByShiftId,
                RequestedWithUserId = source.RequestedWithUserId,
                RequestedWithShiftId = source.RequestedWithShiftId,
                Message = source.Message,
                Status = source.Status,
                RequestedAt = source.RequestedAt,
                ResolvedAt = source.ResolvedAt,
                ResolvedByUserId = source.ResolvedByUserId,
                ResolutionNote = source.ResolutionNote
            };
        }
    }
}
