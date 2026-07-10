using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class AttendanceServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IDeviceRepository> _deviceRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly Mock<IGenericRepository<BioUser>> _userRepoMock;
        private readonly Mock<IGenericRepository<BioPortalUser>> _portalUserRepoMock;
        private readonly AttendanceService _sut;

        public AttendanceServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _deviceRepoMock = new Mock<IDeviceRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _userRepoMock = new Mock<IGenericRepository<BioUser>>();
            _portalUserRepoMock = new Mock<IGenericRepository<BioPortalUser>>();

            _uowMock.Setup(u => u.Devices).Returns(_deviceRepoMock.Object);
            _uowMock.Setup(u => u.Attendance).Returns(_attendanceRepoMock.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.PortalUsers).Returns(_portalUserRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Default: no users found (UserName stays empty); individual tests may override this
            _userRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioUser, bool>>>()))
                .ReturnsAsync(new List<BioUser>());
            _portalUserRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioPortalUser, bool>>>()))
                .ReturnsAsync(new List<BioPortalUser>());

            _sut = new AttendanceService(_uowMock.Object);
        }

        [Fact]
        public async Task ProcessPushAsync_ApprovedDevice_SavesRecords()
        {
            // Arrange
            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 5,
                IsApproved = true,
                IsActive = true
            };
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN001"))
                .ReturnsAsync(device);
            _attendanceRepoMock.Setup(r => r.IsDuplicateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioAttendanceLog>()))
                .Returns(Task.CompletedTask);

            var records = new List<AttendanceRecordDto>
            {
                new AttendanceRecordDto
                {
                    UserCode = "U1",
                    PunchTime = DateTime.Today.AddHours(9),
                    AttendanceState = 0,
                    VerifyMode = 1
                }
            };

            // Act
            var (saved, duplicates) = await _sut.ProcessPushAsync("SN001", records, clientId: null);

            // Assert
            Assert.Equal(1, saved);
            Assert.Equal(0, duplicates);
            _attendanceRepoMock.Verify(r => r.AddAsync(It.Is<BioAttendanceLog>(
                l => l.DeviceSN == "SN001" && l.UserCode == "U1" && l.ClientId == 5)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessPushAsync_UnapprovedDevice_ReturnsZero()
        {
            // Arrange — device exists but IsApproved = false
            var device = new BioDevice
            {
                Id = 2,
                SerialNumber = "SN002",
                IsApproved = false,
                IsActive = true
            };
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN002"))
                .ReturnsAsync(device);

            var records = new List<AttendanceRecordDto>
            {
                new AttendanceRecordDto { UserCode = "U1", PunchTime = DateTime.Now }
            };

            // Act
            var (saved, duplicates) = await _sut.ProcessPushAsync("SN002", records, clientId: null);

            // Assert
            Assert.Equal(0, saved);
            Assert.Equal(0, duplicates);
            _attendanceRepoMock.Verify(r => r.AddAsync(It.IsAny<BioAttendanceLog>()), Times.Never);
        }

        [Fact]
        public async Task ProcessPushAsync_UnknownDevice_ReturnsZero()
        {
            // Arrange — device does not exist
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("UNKNOWN"))
                .ReturnsAsync((BioDevice?)null);

            var records = new List<AttendanceRecordDto>
            {
                new AttendanceRecordDto { UserCode = "U1", PunchTime = DateTime.Now }
            };

            // Act
            var (saved, duplicates) = await _sut.ProcessPushAsync("UNKNOWN", records, clientId: null);

            // Assert
            Assert.Equal(0, saved);
            Assert.Equal(0, duplicates);
        }

        [Fact]
        public async Task ProcessPushAsync_NullRecords_ReturnsZeroWithoutSaving()
        {
            var device = new BioDevice
            {
                Id = 5,
                SerialNumber = "SN005",
                ClientId = 2,
                IsApproved = true,
                IsActive = true
            };
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN005"))
                .ReturnsAsync(device);

            var (saved, duplicates) = await _sut.ProcessPushAsync("SN005", records: null!, clientId: null);

            Assert.Equal(0, saved);
            Assert.Equal(0, duplicates);
            _attendanceRepoMock.Verify(r => r.AddAsync(It.IsAny<BioAttendanceLog>()), Times.Never);
        }

        [Fact]
        public async Task ProcessPushAsync_DuplicateRecord_CountedAsDuplicate()
        {
            // Arrange
            var device = new BioDevice
            {
                Id = 3,
                SerialNumber = "SN003",
                ClientId = 1,
                IsApproved = true,
                IsActive = true
            };
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN003"))
                .ReturnsAsync(device);
            // All records are duplicates
            _attendanceRepoMock.Setup(r => r.IsDuplicateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioAttendanceLog>()))
                .Returns(Task.CompletedTask);

            var records = new List<AttendanceRecordDto>
            {
                new AttendanceRecordDto { UserCode = "U1", PunchTime = DateTime.Now }
            };

            // Act
            var (saved, duplicates) = await _sut.ProcessPushAsync("SN003", records, clientId: null);

            // Assert
            Assert.Equal(0, saved);
            Assert.Equal(1, duplicates);
        }

        [Fact]
        public async Task ProcessPushAsync_ExplicitClientId_OverridesDeviceClientId()
        {
            // Arrange
            var device = new BioDevice
            {
                Id = 4,
                SerialNumber = "SN004",
                ClientId = 99,         // device's stored clientId
                IsApproved = true,
                IsActive = true
            };
            _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN004"))
                .ReturnsAsync(device);
            _attendanceRepoMock.Setup(r => r.IsDuplicateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            BioAttendanceLog? capturedLog = null;
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioAttendanceLog>()))
                .Callback<BioAttendanceLog>(l => capturedLog = l)
                .Returns(Task.CompletedTask);

            var records = new List<AttendanceRecordDto>
            {
                new AttendanceRecordDto { UserCode = "U1", PunchTime = DateTime.Now }
            };

            // Act — caller passes explicit clientId = 7
            await _sut.ProcessPushAsync("SN004", records, clientId: 7);

            // Assert — record stored with the caller-supplied clientId
            Assert.NotNull(capturedLog);
            Assert.Equal(7, capturedLog!.ClientId);
        }

        [Fact]
        public async Task GetTodayAsync_DelegatesToRepository()
        {
            // Arrange
            var today = DateTime.Today;
            var logs = new List<BioAttendanceLog>
            {
                new BioAttendanceLog
                {
                    Id = 1,
                    DeviceSN = "SN001",
                    UserCode = "U1",
                    PunchTime = today.AddHours(9),
                    CreatedOn = today
                }
            };
            _attendanceRepoMock.Setup(r => r.GetTodayLogsAsync(null))
                .ReturnsAsync(logs);

            // Act
            var result = (await _sut.GetTodayAsync(clientId: null)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("U1", result[0].UserCode);
        }

        [Fact]
        public async Task GetClientAttendanceReportAsync_UsesClientTimezoneAndComputesFirstInLastOut()
        {
            // Arrange
            _portalUserRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioPortalUser, bool>>>()))
                .ReturnsAsync(new List<BioPortalUser>
                {
                    new BioPortalUser { ClientId = 10, IsActive = true, TimeZoneId = "Asia/Kolkata", CreatedOn = DateTime.UtcNow }
                });

            _attendanceRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioAttendanceLog, bool>>>()))
                .ReturnsAsync(new List<BioAttendanceLog>
                {
                    // 2026-07-03 local (Asia/Kolkata)
                    new BioAttendanceLog { ClientId = 10, UserCode = "E001", UserName = "Alice", PunchTime = new DateTime(2026, 7, 3, 3, 30, 0, DateTimeKind.Utc), AttendanceState = 0 },
                    new BioAttendanceLog { ClientId = 10, UserCode = "E001", UserName = "Alice", PunchTime = new DateTime(2026, 7, 3, 11, 30, 0, DateTimeKind.Utc), AttendanceState = 1 },
                    // another user same day
                    new BioAttendanceLog { ClientId = 10, UserCode = "E002", UserName = "Bob", PunchTime = new DateTime(2026, 7, 3, 4, 0, 0, DateTimeKind.Utc), AttendanceState = 0 },
                    new BioAttendanceLog { ClientId = 10, UserCode = "E002", UserName = "Bob", PunchTime = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc), AttendanceState = 1 }
                });

            // Act
            var report = await _sut.GetClientAttendanceReportAsync(
                clientId: 10,
                period: AttendanceReportPeriod.Daily,
                referenceDate: new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.Equal("Asia/Kolkata", report.TimeZoneId);
            Assert.Equal("daily", report.Period);
            Assert.Equal(2, report.Rows.Count);

            var alice = report.Rows.First(r => r.UserCode == "E001");
            Assert.Equal("2026-07-03", alice.Date);
            Assert.Equal("2026-07-03 09:00:00", alice.FirstIn);
            Assert.Equal("2026-07-03 17:00:00", alice.LastOut);
            Assert.Equal(2, alice.PunchCount);
        }

        [Fact]
        public async Task GetClientAttendanceReportAsync_FallsBackToUtc_WhenTimezoneInvalid()
        {
            _portalUserRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioPortalUser, bool>>>()))
                .ReturnsAsync(new List<BioPortalUser>
                {
                    new BioPortalUser { ClientId = 11, IsActive = true, TimeZoneId = "Invalid/Timezone" }
                });

            _attendanceRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioAttendanceLog, bool>>>()))
                .ReturnsAsync(new List<BioAttendanceLog>());

            var report = await _sut.GetClientAttendanceReportAsync(
                clientId: 11,
                period: AttendanceReportPeriod.Weekly,
                referenceDate: new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal("UTC", report.TimeZoneId);
            Assert.Equal("weekly", report.Period);
        }
    }
}
