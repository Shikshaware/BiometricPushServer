using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;

namespace BiometricPushServer.Service
{
    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork _uow;

        public ShiftService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<ShiftDto>> GetAllAsync(int? clientId = null)
        {
            var items = await _uow.Shifts.FindAsync(s =>
                s.IsActive && (clientId == null || s.ClientId == clientId));
            return items.Select(MapToDto);
        }

        public async Task<ShiftDto?> GetByIdAsync(int id)
        {
            var shift = await _uow.Shifts.GetByIdAsync(id);
            return shift == null ? null : MapToDto(shift);
        }

        public async Task<ShiftDto> CreateAsync(ShiftDto dto)
        {
            var shift = new BioShift
            {
                ClientId = dto.ClientId,
                Name = dto.Name,
                StartTime = ParseTime(dto.StartTime),
                EndTime = ParseTime(dto.EndTime),
                GracePeriodMinutes = dto.GracePeriodMinutes,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.Shifts.AddAsync(shift);
            await _uow.SaveChangesAsync();
            return MapToDto(shift);
        }

        public async Task<ShiftDto?> UpdateAsync(int id, ShiftDto dto)
        {
            var shift = await _uow.Shifts.GetByIdAsync(id);
            if (shift == null) return null;

            shift.Name = dto.Name;
            shift.StartTime = ParseTime(dto.StartTime);
            shift.EndTime = ParseTime(dto.EndTime);
            shift.GracePeriodMinutes = dto.GracePeriodMinutes;
            shift.IsActive = dto.IsActive;
            if (dto.ClientId.HasValue) shift.ClientId = dto.ClientId;

            _uow.Shifts.Update(shift);
            await _uow.SaveChangesAsync();
            return MapToDto(shift);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var shift = await _uow.Shifts.GetByIdAsync(id);
            if (shift == null) return false;
            _uow.Shifts.Remove(shift);
            await _uow.SaveChangesAsync();
            return true;
        }

        private static TimeSpan ParseTime(string value)
        {
            if (TimeSpan.TryParseExact(value, @"HH\:mm", null, out var ts)) return ts;
            if (TimeSpan.TryParseExact(value, @"H\:mm", null, out ts)) return ts;
            if (TimeSpan.TryParse(value, out ts)) return ts;
            return TimeSpan.Zero;
        }

        private static ShiftDto MapToDto(BioShift s) => new ShiftDto
        {
            Id = s.Id,
            ClientId = s.ClientId,
            Name = s.Name,
            StartTime = s.StartTime.ToString(@"hh\:mm"),
            EndTime = s.EndTime.ToString(@"hh\:mm"),
            GracePeriodMinutes = s.GracePeriodMinutes,
            IsActive = s.IsActive
        };
    }

    public class HolidayService : IHolidayService
    {
        private readonly IUnitOfWork _uow;

        public HolidayService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<HolidayDto>> GetAllAsync(int? clientId = null, int? year = null)
        {
            var items = await _uow.Holidays.FindAsync(h =>
                (clientId == null || h.ClientId == clientId) &&
                (year == null || h.HolidayDate.Year == year.Value || h.IsRecurring));
            return items
                .OrderBy(h => h.HolidayDate)
                .Select(MapToDto);
        }

        public async Task<HolidayDto?> GetByIdAsync(int id)
        {
            var holiday = await _uow.Holidays.GetByIdAsync(id);
            return holiday == null ? null : MapToDto(holiday);
        }

        public async Task<HolidayDto> CreateAsync(HolidayDto dto)
        {
            var holiday = new BioHoliday
            {
                ClientId = dto.ClientId,
                Name = dto.Name,
                HolidayDate = dto.HolidayDate.Date,
                IsRecurring = dto.IsRecurring,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.Holidays.AddAsync(holiday);
            await _uow.SaveChangesAsync();
            return MapToDto(holiday);
        }

        public async Task<HolidayDto?> UpdateAsync(int id, HolidayDto dto)
        {
            var holiday = await _uow.Holidays.GetByIdAsync(id);
            if (holiday == null) return null;

            holiday.Name = dto.Name;
            holiday.HolidayDate = dto.HolidayDate.Date;
            holiday.IsRecurring = dto.IsRecurring;
            if (dto.ClientId.HasValue) holiday.ClientId = dto.ClientId;

            _uow.Holidays.Update(holiday);
            await _uow.SaveChangesAsync();
            return MapToDto(holiday);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var holiday = await _uow.Holidays.GetByIdAsync(id);
            if (holiday == null) return false;
            _uow.Holidays.Remove(holiday);
            await _uow.SaveChangesAsync();
            return true;
        }

        private static HolidayDto MapToDto(BioHoliday h) => new HolidayDto
        {
            Id = h.Id,
            ClientId = h.ClientId,
            Name = h.Name,
            HolidayDate = h.HolidayDate,
            IsRecurring = h.IsRecurring
        };
    }

    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _uow;

        public ScheduleService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<EmployeeScheduleDto>> GetAllAsync(int? clientId = null, int? userId = null)
        {
            var schedules = await _uow.EmployeeSchedules.FindAsync(s =>
                (clientId == null || s.ClientId == clientId) &&
                (userId == null || s.UserId == userId));

            var shiftIds = schedules.Where(s => s.ShiftId.HasValue).Select(s => s.ShiftId!.Value).Distinct().ToList();
            var shifts = shiftIds.Count > 0
                ? (await _uow.Shifts.FindAsync(s => shiftIds.Contains(s.Id))).ToDictionary(s => s.Id, s => s.Name)
                : new Dictionary<int, string>();

            return schedules
                .OrderBy(s => s.EffectiveFrom)
                .Select(s => MapToDto(s, shifts));
        }

        public async Task<EmployeeScheduleDto?> GetByIdAsync(int id)
        {
            var schedule = await _uow.EmployeeSchedules.GetByIdAsync(id);
            if (schedule == null) return null;

            var shiftName = string.Empty;
            if (schedule.ShiftId.HasValue)
            {
                var shift = await _uow.Shifts.GetByIdAsync(schedule.ShiftId.Value);
                shiftName = shift?.Name ?? string.Empty;
            }

            return MapToDto(schedule, shiftName);
        }

        public async Task<EmployeeScheduleDto> CreateAsync(EmployeeScheduleDto dto)
        {
            var schedule = new BioEmployeeSchedule
            {
                ClientId = dto.ClientId,
                UserId = dto.UserId,
                ShiftId = dto.ShiftId,
                EffectiveFrom = dto.EffectiveFrom.Date,
                EffectiveTo = dto.EffectiveTo.HasValue ? dto.EffectiveTo.Value.Date : null,
                IsActive = dto.IsActive,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.EmployeeSchedules.AddAsync(schedule);
            await _uow.SaveChangesAsync();

            var shiftName = string.Empty;
            if (schedule.ShiftId.HasValue)
            {
                var shift = await _uow.Shifts.GetByIdAsync(schedule.ShiftId.Value);
                shiftName = shift?.Name ?? string.Empty;
            }

            return MapToDto(schedule, shiftName);
        }

        public async Task<EmployeeScheduleDto?> UpdateAsync(int id, EmployeeScheduleDto dto)
        {
            var schedule = await _uow.EmployeeSchedules.GetByIdAsync(id);
            if (schedule == null) return null;

            schedule.ShiftId = dto.ShiftId;
            schedule.EffectiveFrom = dto.EffectiveFrom.Date;
            schedule.EffectiveTo = dto.EffectiveTo.HasValue ? dto.EffectiveTo.Value.Date : null;
            schedule.IsActive = dto.IsActive;
            if (dto.ClientId.HasValue) schedule.ClientId = dto.ClientId;

            _uow.EmployeeSchedules.Update(schedule);
            await _uow.SaveChangesAsync();

            var shiftName = string.Empty;
            if (schedule.ShiftId.HasValue)
            {
                var shift = await _uow.Shifts.GetByIdAsync(schedule.ShiftId.Value);
                shiftName = shift?.Name ?? string.Empty;
            }

            return MapToDto(schedule, shiftName);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var schedule = await _uow.EmployeeSchedules.GetByIdAsync(id);
            if (schedule == null) return false;
            _uow.EmployeeSchedules.Remove(schedule);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<EmployeeScheduleDto?> GetActiveForUserAsync(int userId, DateTime? date = null)
        {
            var refDate = (date ?? DateTime.UtcNow).Date;
            var schedules = await _uow.EmployeeSchedules.FindAsync(s =>
                s.UserId == userId &&
                s.IsActive &&
                s.EffectiveFrom <= refDate &&
                (s.EffectiveTo == null || s.EffectiveTo.Value >= refDate));

            var active = schedules.OrderByDescending(s => s.EffectiveFrom).FirstOrDefault();
            if (active == null) return null;

            var shiftName = string.Empty;
            if (active.ShiftId.HasValue)
            {
                var shift = await _uow.Shifts.GetByIdAsync(active.ShiftId.Value);
                shiftName = shift?.Name ?? string.Empty;
            }

            return MapToDto(active, shiftName);
        }

        private static EmployeeScheduleDto MapToDto(BioEmployeeSchedule s, Dictionary<int, string> shifts) =>
            MapToDto(s, s.ShiftId.HasValue && shifts.TryGetValue(s.ShiftId.Value, out var name) ? name : string.Empty);

        private static EmployeeScheduleDto MapToDto(BioEmployeeSchedule s, string shiftName) => new EmployeeScheduleDto
        {
            Id = s.Id,
            ClientId = s.ClientId,
            UserId = s.UserId,
            ShiftId = s.ShiftId,
            ShiftName = shiftName,
            EffectiveFrom = s.EffectiveFrom,
            EffectiveTo = s.EffectiveTo,
            IsActive = s.IsActive
        };
    }
}
