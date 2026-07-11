using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class ShiftServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGenericRepository<BioShift>> _shiftRepoMock;
        private readonly ShiftService _sut;

        public ShiftServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _shiftRepoMock = new Mock<IGenericRepository<BioShift>>();
            _uowMock.Setup(u => u.Shifts).Returns(_shiftRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _sut = new ShiftService(_uowMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsActiveShiftsForClient()
        {
            // Arrange
            var shifts = new List<BioShift>
            {
                new BioShift { Id = 1, ClientId = 5, Name = "Morning", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsActive = true },
                new BioShift { Id = 2, ClientId = 5, Name = "Night",   StartTime = new TimeSpan(21, 0, 0), EndTime = new TimeSpan(5, 0, 0),  IsActive = false }
            };
            _shiftRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioShift, bool>>>()))
                .ReturnsAsync(shifts.Where(s => s.IsActive && s.ClientId == 5).ToList());

            // Act
            var result = (await _sut.GetAllAsync(clientId: 5)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Morning", result[0].Name);
            Assert.Equal("09:00", result[0].StartTime);
            Assert.Equal("17:00", result[0].EndTime);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            // Arrange
            _shiftRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new BioShift { Id = 1, Name = "Day", StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(16, 30, 0), GracePeriodMinutes = 10, IsActive = true });

            // Act
            var result = await _sut.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
            Assert.Equal("Day", result.Name);
            Assert.Equal("08:30", result.StartTime);
            Assert.Equal("16:30", result.EndTime);
            Assert.Equal(10, result.GracePeriodMinutes);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            _shiftRepoMock.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((BioShift?)null);

            // Act
            var result = await _sut.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_PersistsShiftAndReturnsDto()
        {
            // Arrange
            BioShift? captured = null;
            _shiftRepoMock.Setup(r => r.AddAsync(It.IsAny<BioShift>()))
                .Callback<BioShift>(s => captured = s)
                .Returns(Task.CompletedTask);

            var dto = new ShiftDto { ClientId = 3, Name = "Evening", StartTime = "17:00", EndTime = "01:00", GracePeriodMinutes = 5 };

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("Evening", captured!.Name);
            Assert.Equal(new TimeSpan(17, 0, 0), captured.StartTime);
            Assert.Equal(new TimeSpan(1, 0, 0), captured.EndTime);
            Assert.Equal(5, captured.GracePeriodMinutes);
            Assert.True(captured.IsActive);
            Assert.Equal("17:00", result.StartTime);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAndReturnsDto_WhenFound()
        {
            // Arrange
            var existing = new BioShift { Id = 2, Name = "Old", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), IsActive = true };
            _shiftRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);
            _shiftRepoMock.Setup(r => r.Update(It.IsAny<BioShift>()));

            var dto = new ShiftDto { Name = "New", StartTime = "09:00", EndTime = "18:00", GracePeriodMinutes = 15, IsActive = true };

            // Act
            var result = await _sut.UpdateAsync(2, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New", result!.Name);
            Assert.Equal("09:00", result.StartTime);
            Assert.Equal("18:00", result.EndTime);
            Assert.Equal(15, result.GracePeriodMinutes);
            _shiftRepoMock.Verify(r => r.Update(existing), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            _shiftRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioShift?)null);

            // Act
            var result = await _sut.UpdateAsync(99, new ShiftDto { Name = "X", StartTime = "09:00", EndTime = "17:00" });

            // Assert
            Assert.Null(result);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_RemovesShiftAndReturnsTrue_WhenFound()
        {
            // Arrange
            var shift = new BioShift { Id = 3, Name = "Shift" };
            _shiftRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(shift);
            _shiftRepoMock.Setup(r => r.Remove(It.IsAny<BioShift>()));

            // Act
            var result = await _sut.DeleteAsync(3);

            // Assert
            Assert.True(result);
            _shiftRepoMock.Verify(r => r.Remove(shift), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            _shiftRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioShift?)null);

            // Act
            var result = await _sut.DeleteAsync(99);

            // Assert
            Assert.False(result);
            _shiftRepoMock.Verify(r => r.Remove(It.IsAny<BioShift>()), Times.Never);
        }
    }

    public class HolidayServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGenericRepository<BioHoliday>> _holidayRepoMock;
        private readonly HolidayService _sut;

        public HolidayServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _holidayRepoMock = new Mock<IGenericRepository<BioHoliday>>();
            _uowMock.Setup(u => u.Holidays).Returns(_holidayRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _sut = new HolidayService(_uowMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsHolidaysOrderedByDate()
        {
            // Arrange
            var holidays = new List<BioHoliday>
            {
                new BioHoliday { Id = 1, ClientId = 1, Name = "New Year", HolidayDate = new DateTime(2026, 1, 1), IsRecurring = true },
                new BioHoliday { Id = 2, ClientId = 1, Name = "Labour Day", HolidayDate = new DateTime(2026, 5, 1), IsRecurring = false }
            };
            _holidayRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioHoliday, bool>>>()))
                .ReturnsAsync(holidays);

            // Act
            var result = (await _sut.GetAllAsync(clientId: 1)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("New Year", result[0].Name);
            Assert.Equal("Labour Day", result[1].Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            // Arrange
            var date = new DateTime(2026, 12, 25);
            _holidayRepoMock.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(new BioHoliday { Id = 5, ClientId = 2, Name = "Christmas", HolidayDate = date, IsRecurring = true });

            // Act
            var result = await _sut.GetByIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Christmas", result!.Name);
            Assert.Equal(date, result.HolidayDate);
            Assert.True(result.IsRecurring);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            _holidayRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioHoliday?)null);

            var result = await _sut.GetByIdAsync(99);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_PersistsHolidayWithDateOnly()
        {
            // Arrange — pass a full datetime; service should store only the Date part
            BioHoliday? captured = null;
            _holidayRepoMock.Setup(r => r.AddAsync(It.IsAny<BioHoliday>()))
                .Callback<BioHoliday>(h => captured = h)
                .Returns(Task.CompletedTask);

            var dto = new HolidayDto { ClientId = 1, Name = "Diwali", HolidayDate = new DateTime(2026, 10, 20, 12, 30, 0), IsRecurring = false };

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(new DateTime(2026, 10, 20), captured!.HolidayDate); // time stripped
            Assert.Equal("Diwali", result.Name);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAndReturnsDto_WhenFound()
        {
            // Arrange
            var existing = new BioHoliday { Id = 4, Name = "Old", HolidayDate = new DateTime(2026, 3, 1), IsRecurring = false };
            _holidayRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);
            _holidayRepoMock.Setup(r => r.Update(It.IsAny<BioHoliday>()));

            var dto = new HolidayDto { Name = "Updated", HolidayDate = new DateTime(2026, 3, 15, 0, 0, 0), IsRecurring = true };

            // Act
            var result = await _sut.UpdateAsync(4, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated", result!.Name);
            Assert.Equal(new DateTime(2026, 3, 15), result.HolidayDate);
            Assert.True(result.IsRecurring);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenNotFound()
        {
            _holidayRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioHoliday?)null);

            var result = await _sut.UpdateAsync(99, new HolidayDto { Name = "X", HolidayDate = DateTime.Today });

            Assert.Null(result);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_RemovesAndReturnsTrue_WhenFound()
        {
            var holiday = new BioHoliday { Id = 6, Name = "H" };
            _holidayRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(holiday);
            _holidayRepoMock.Setup(r => r.Remove(It.IsAny<BioHoliday>()));

            var result = await _sut.DeleteAsync(6);

            Assert.True(result);
            _holidayRepoMock.Verify(r => r.Remove(holiday), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            _holidayRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioHoliday?)null);

            var result = await _sut.DeleteAsync(99);

            Assert.False(result);
        }
    }

    public class ScheduleServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGenericRepository<BioEmployeeSchedule>> _scheduleRepoMock;
        private readonly Mock<IGenericRepository<BioShift>> _shiftRepoMock;
        private readonly ScheduleService _sut;

        public ScheduleServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _scheduleRepoMock = new Mock<IGenericRepository<BioEmployeeSchedule>>();
            _shiftRepoMock = new Mock<IGenericRepository<BioShift>>();
            _uowMock.Setup(u => u.EmployeeSchedules).Returns(_scheduleRepoMock.Object);
            _uowMock.Setup(u => u.Shifts).Returns(_shiftRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _sut = new ScheduleService(_uowMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ResolvesShiftNamesFromRepository()
        {
            // Arrange
            var schedules = new List<BioEmployeeSchedule>
            {
                new BioEmployeeSchedule { Id = 1, UserId = 10, ShiftId = 2, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true },
                new BioEmployeeSchedule { Id = 2, UserId = 11, ShiftId = null, EffectiveFrom = new DateTime(2026, 2, 1), IsActive = true }
            };
            _scheduleRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioEmployeeSchedule, bool>>>()))
                .ReturnsAsync(schedules);
            _shiftRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioShift, bool>>>()))
                .ReturnsAsync(new List<BioShift> { new BioShift { Id = 2, Name = "Morning" } });

            // Act
            var result = (await _sut.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Morning", result.First(r => r.UserId == 10).ShiftName);
            Assert.Equal(string.Empty, result.First(r => r.UserId == 11).ShiftName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenFound()
        {
            // Arrange
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new BioEmployeeSchedule { Id = 1, UserId = 5, ShiftId = 3, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(new BioShift { Id = 3, Name = "Night" });

            // Act
            var result = await _sut.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result!.UserId);
            Assert.Equal(3, result.ShiftId);
            Assert.Equal("Night", result.ShiftName);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioEmployeeSchedule?)null);

            var result = await _sut.GetByIdAsync(99);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_PersistsScheduleWithDatePartsOnly()
        {
            // Arrange — pass datetime with time; service should store only the Date part
            BioEmployeeSchedule? captured = null;
            _scheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<BioEmployeeSchedule>()))
                .Callback<BioEmployeeSchedule>(s => captured = s)
                .Returns(Task.CompletedTask);
            _shiftRepoMock.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new BioShift { Id = 2, Name = "Day" });

            var dto = new EmployeeScheduleDto
            {
                ClientId = 1, UserId = 7, ShiftId = 2,
                EffectiveFrom = new DateTime(2026, 6, 1, 10, 30, 0),
                EffectiveTo = new DateTime(2026, 12, 31, 23, 59, 59),
                IsActive = true
            };

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(new DateTime(2026, 6, 1), captured!.EffectiveFrom);    // time stripped
            Assert.Equal(new DateTime(2026, 12, 31), captured.EffectiveTo);    // time stripped
            Assert.Equal("Day", result.ShiftName);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAndReturnsDto_WhenFound()
        {
            // Arrange
            var existing = new BioEmployeeSchedule { Id = 2, UserId = 8, ShiftId = 1, EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true };
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);
            _scheduleRepoMock.Setup(r => r.Update(It.IsAny<BioEmployeeSchedule>()));
            _shiftRepoMock.Setup(r => r.GetByIdAsync(4))
                .ReturnsAsync(new BioShift { Id = 4, Name = "Evening" });

            var dto = new EmployeeScheduleDto { UserId = 8, ShiftId = 4, EffectiveFrom = new DateTime(2026, 3, 1), IsActive = false };

            // Act
            var result = await _sut.UpdateAsync(2, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result!.ShiftId);
            Assert.Equal("Evening", result.ShiftName);
            Assert.False(result.IsActive);
            _scheduleRepoMock.Verify(r => r.Update(existing), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenNotFound()
        {
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioEmployeeSchedule?)null);

            var result = await _sut.UpdateAsync(99, new EmployeeScheduleDto { EffectiveFrom = DateTime.Today });

            Assert.Null(result);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_RemovesAndReturnsTrue_WhenFound()
        {
            var schedule = new BioEmployeeSchedule { Id = 3, UserId = 9 };
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(schedule);
            _scheduleRepoMock.Setup(r => r.Remove(It.IsAny<BioEmployeeSchedule>()));

            var result = await _sut.DeleteAsync(3);

            Assert.True(result);
            _scheduleRepoMock.Verify(r => r.Remove(schedule), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
        {
            _scheduleRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BioEmployeeSchedule?)null);

            var result = await _sut.DeleteAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task GetActiveForUserAsync_ReturnsMostRecentActiveSchedule_OverlappingDate()
        {
            // Arrange — two active overlapping schedules; most recent EffectiveFrom wins
            var refDate = new DateTime(2026, 7, 1);
            var schedules = new List<BioEmployeeSchedule>
            {
                new BioEmployeeSchedule { Id = 10, UserId = 20, ShiftId = 1, EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = null, IsActive = true },
                new BioEmployeeSchedule { Id = 11, UserId = 20, ShiftId = 2, EffectiveFrom = new DateTime(2026, 6, 1), EffectiveTo = null, IsActive = true }
            };
            _scheduleRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioEmployeeSchedule, bool>>>()))
                .ReturnsAsync(schedules);
            _shiftRepoMock.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new BioShift { Id = 2, Name = "Afternoon" });

            // Act
            var result = await _sut.GetActiveForUserAsync(userId: 20, date: refDate);

            // Assert — most recent EffectiveFrom (June) wins over January
            Assert.NotNull(result);
            Assert.Equal(11, result!.Id);
            Assert.Equal("Afternoon", result.ShiftName);
        }

        [Fact]
        public async Task GetActiveForUserAsync_ReturnsNull_WhenNoActiveScheduleExistsForDate()
        {
            // Arrange — schedule ended before refDate
            var schedules = new List<BioEmployeeSchedule>
            {
                new BioEmployeeSchedule { Id = 12, UserId = 21, ShiftId = 1, EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 3, 31), IsActive = true }
            };
            _scheduleRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioEmployeeSchedule, bool>>>()))
                .ReturnsAsync(new List<BioEmployeeSchedule>()); // predicate filters it out

            // Act
            var result = await _sut.GetActiveForUserAsync(userId: 21, date: new DateTime(2026, 7, 1));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveForUserAsync_DefaultsToUtcNow_WhenDateNotProvided()
        {
            // Arrange — one active schedule with no end date
            var schedule = new BioEmployeeSchedule { Id = 13, UserId = 22, ShiftId = 3, EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = null, IsActive = true };
            _scheduleRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioEmployeeSchedule, bool>>>()))
                .ReturnsAsync(new List<BioEmployeeSchedule> { schedule });
            _shiftRepoMock.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(new BioShift { Id = 3, Name = "Night" });

            // Act — no date provided, service must not throw
            var result = await _sut.GetActiveForUserAsync(userId: 22);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(13, result!.Id);
        }
    }
}
