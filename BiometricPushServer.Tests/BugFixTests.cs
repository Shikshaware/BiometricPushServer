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
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Controllers.Api;
using BiometricPushServer.Web.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class BugFixTests
    {
        // ── Bug 3: DashboardService threshold ────────────────────────────────

        [Fact]
        public async Task DashboardService_UsesOfflineThresholdConstant()
        {
            // Arrange: two devices — one heartbeat just inside threshold, one just outside
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            var attendanceRepo = new Mock<IAttendanceRepository>();
            var commandRepo = new Mock<ICommandRepository>();

            var threshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            var devices = new List<BioDevice>
            {
                new BioDevice { Id = 1, LastHeartbeatOn = threshold.AddSeconds(30) },  // online
                new BioDevice { Id = 2, LastHeartbeatOn = threshold.AddSeconds(-30) }  // offline
            };

            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);
            uow.Setup(u => u.Attendance).Returns(attendanceRepo.Object);
            uow.Setup(u => u.Commands).Returns(commandRepo.Object);
            deviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioDevice, bool>>>()))
                .ReturnsAsync(devices);
            attendanceRepo.Setup(r => r.GetTodayLogsAsync(null))
                .ReturnsAsync(new List<BioAttendanceLog>());
            commandRepo.Setup(r => r.GetAllPendingAsync())
                .ReturnsAsync(new List<BioDeviceCommand>());
            uow.Setup(u => u.Users).Returns(new Mock<IGenericRepository<BioUser>>().Object);
            uow.Setup(u => u.Users.CountAsync(It.IsAny<Expression<Func<BioUser, bool>>>()))
                .ReturnsAsync(0);

            var service = new DashboardService(uow.Object);

            // Act
            var stats = await service.GetStatsAsync(clientId: null);

            // Assert: only the device whose heartbeat is within the constant threshold is online
            Assert.Equal(1, stats.OnlineDevices);
            Assert.Equal(1, stats.OfflineDevices);
        }

        // ── Bug 5: CommandService expiry constant ─────────────────────────────

        [Fact]
        public async Task CommandService_EnqueueAsync_UsesDefaultCommandTimeoutMinutes()
        {
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            var commandRepo = new Mock<ICommandRepository>();

            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);
            uow.Setup(u => u.Commands).Returns(commandRepo.Object);
            deviceRepo.Setup(r => r.GetBySerialNumberAsync("SN1"))
                .ReturnsAsync(new BioDevice { Id = 1, SerialNumber = "SN1" });
            uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            BioDeviceCommand? captured = null;
            commandRepo.Setup(r => r.AddAsync(It.IsAny<BioDeviceCommand>()))
                .Callback<BioDeviceCommand>(c => captured = c)
                .Returns(Task.CompletedTask);

            var service = new CommandService(uow.Object);
            var before = DateTime.UtcNow;
            await service.EnqueueAsync("SN1", "RESTART");
            var after = DateTime.UtcNow;

            Assert.NotNull(captured);
            var expectedExpiry = before.AddMinutes(AppConstants.DefaultCommandTimeoutMinutes);
            Assert.True(captured!.ExpiresOn >= expectedExpiry.AddSeconds(-2),
                "ExpiresOn should be approximately DefaultCommandTimeoutMinutes from now");
            Assert.True(captured.ExpiresOn <= after.AddMinutes(AppConstants.DefaultCommandTimeoutMinutes).AddSeconds(2));
        }

        // ── Bug 6: Sent-but-unexecuted commands expire ────────────────────────

        [Fact]
        public async Task ExpireStaleCommandsAsync_MarksSentExpiredCommandsAsFailed()
        {
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();
            var uow = new Mock<IUnitOfWork>();
            var logger = new Mock<ILogger<BiometricBackgroundJobs>>();

            commandService.Setup(s => s.GetAllPendingAsync())
                .ReturnsAsync(new List<BioDeviceCommand>());

            var stuckCmd = new BioDeviceCommand
            {
                Id = 42,
                DeviceSN = "SN1",
                CommandType = "RESTART",
                IsSent = true,
                IsExecuted = false,
                IsFailed = false,
                ExpiresOn = DateTime.UtcNow.AddMinutes(-1)  // already past expiry
            };
            commandService.Setup(s => s.GetSentExpiredAsync())
                .ReturnsAsync(new[] { stuckCmd });

            var jobs = new BiometricBackgroundJobs(
                deviceService.Object, commandService.Object, uow.Object, logger.Object);

            await jobs.ExpireStaleCommandsAsync();

            commandService.Verify(s =>
                s.MarkFailedAsync(42, It.Is<string>(r => r.Contains("acknowledge"))),
                Times.Once);
        }

        [Fact]
        public async Task ExpireStaleCommandsAsync_NoSentExpiredCommands_DoesNotCallMarkFailed()
        {
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();
            var uow = new Mock<IUnitOfWork>();
            var logger = new Mock<ILogger<BiometricBackgroundJobs>>();

            commandService.Setup(s => s.GetAllPendingAsync())
                .ReturnsAsync(new List<BioDeviceCommand>());
            commandService.Setup(s => s.GetSentExpiredAsync())
                .ReturnsAsync(new List<BioDeviceCommand>());

            var jobs = new BiometricBackgroundJobs(
                deviceService.Object, commandService.Object, uow.Object, logger.Object);

            await jobs.ExpireStaleCommandsAsync();

            commandService.Verify(s => s.MarkFailedAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        // ── Bug 7: DeviceDto.IsLocked ─────────────────────────────────────────

        [Fact]
        public async Task DeviceService_GetDeviceDtoAsync_ExposesIsLocked()
        {
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);

            var device = new BioDevice
            {
                Id = 5,
                SerialNumber = "SN5",
                IsLocked = true,
                LastHeartbeatOn = DateTime.UtcNow
            };
            deviceRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(device);

            var service = new DeviceService(uow.Object);
            var dto = await service.GetDeviceDtoAsync(5);

            Assert.NotNull(dto);
            Assert.True(dto!.IsLocked);
        }

        [Fact]
        public async Task DeviceService_GetAllDevicesAsync_ExposesIsLocked()
        {
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);

            var devices = new List<BioDevice>
            {
                new BioDevice { Id = 1, SerialNumber = "SN1", IsLocked = false },
                new BioDevice { Id = 2, SerialNumber = "SN2", IsLocked = true  }
            };
            deviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioDevice, bool>>>()))
                .ReturnsAsync(devices);

            var service = new DeviceService(uow.Object);
            var dtos = (await service.GetAllDevicesAsync()).ToList();

            Assert.False(dtos[0].IsLocked);
            Assert.True(dtos[1].IsLocked);
        }

        // ── Bug 4: DeviceApiController.Lock/Unlock null handling ──────────────

        [Fact]
        public async Task DeviceApiController_Lock_ReturnsNotFound_WhenDeviceDoesNotExist()
        {
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetDeviceDtoAsync(999))
                .ReturnsAsync((DeviceDto?)null);

            var controller = new DeviceApiController(deviceService.Object, commandService.Object);

            var result = await controller.Lock(999);

            Assert.IsType<NotFoundObjectResult>(result);
            // SetLockedAsync and EnqueueAsync must never be called for non-existent device
            deviceService.Verify(s => s.SetLockedAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            commandService.Verify(s => s.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task DeviceApiController_Unlock_ReturnsNotFound_WhenDeviceDoesNotExist()
        {
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetDeviceDtoAsync(999))
                .ReturnsAsync((DeviceDto?)null);

            var controller = new DeviceApiController(deviceService.Object, commandService.Object);

            var result = await controller.Unlock(999);

            Assert.IsType<NotFoundObjectResult>(result);
            deviceService.Verify(s => s.SetLockedAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            commandService.Verify(s => s.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task DeviceApiController_Lock_EnqueuesLockCommandWithCorrectSN()
        {
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetDeviceDtoAsync(1))
                .ReturnsAsync(new DeviceDto { Id = 1, SerialNumber = "SN1" });
            deviceService.Setup(s => s.SetLockedAsync(1, true)).ReturnsAsync(true);
            commandService.Setup(s => s.EnqueueAsync("SN1", "LOCK", null, null, null))
                .ReturnsAsync(new BioDeviceCommand());

            var controller = new DeviceApiController(deviceService.Object, commandService.Object);

            var result = await controller.Lock(1);

            Assert.IsType<OkObjectResult>(result);
            commandService.Verify(s => s.EnqueueAsync("SN1", "LOCK", null, null, null), Times.Once);
        }

        // ── Bug 8: PagedResult.TotalPages divide-by-zero ──────────────────────

        [Fact]
        public void PagedResult_TotalPages_ReturnsZero_WhenPageSizeIsZero()
        {
            var result = new PagedResult<int>
            {
                TotalCount = 100,
                PageSize = 0
            };

            Assert.Equal(0, result.TotalPages);
        }

        [Fact]
        public void PagedResult_TotalPages_CalculatesCorrectly_WithNormalValues()
        {
            var result = new PagedResult<int>
            {
                TotalCount = 101,
                PageSize = 50
            };

            Assert.Equal(3, result.TotalPages);
        }

        // ── Missing: UserName populated in attendance log ─────────────────────

        [Fact]
        public async Task ProcessPushAsync_PopulatesUserName_FromBioUser()
        {
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            var attendanceRepo = new Mock<IAttendanceRepository>();
            var userRepo = new Mock<IGenericRepository<BioUser>>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 5,
                IsApproved = true
            };
            deviceRepo.Setup(r => r.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            attendanceRepo.Setup(r => r.IsDuplicateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            userRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioUser, bool>>>()))
                .ReturnsAsync(new List<BioUser>
                {
                    new BioUser { UserCode = "U1", Name = "Alice Smith", ClientId = 5 }
                });

            BioAttendanceLog? capturedLog = null;
            attendanceRepo.Setup(r => r.AddAsync(It.IsAny<BioAttendanceLog>()))
                .Callback<BioAttendanceLog>(l => capturedLog = l)
                .Returns(Task.CompletedTask);

            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);
            uow.Setup(u => u.Attendance).Returns(attendanceRepo.Object);
            uow.Setup(u => u.Users).Returns(userRepo.Object);
            uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var service = new AttendanceService(uow.Object);
            await service.ProcessPushAsync("SN001", new[]
            {
                new AttendanceRecordDto { UserCode = "U1", PunchTime = DateTime.UtcNow }
            }, clientId: null);

            Assert.NotNull(capturedLog);
            Assert.Equal("Alice Smith", capturedLog!.UserName);
        }

        [Fact]
        public async Task ProcessPushAsync_UserNameEmptyString_WhenUserNotFound()
        {
            var uow = new Mock<IUnitOfWork>();
            var deviceRepo = new Mock<IDeviceRepository>();
            var attendanceRepo = new Mock<IAttendanceRepository>();
            var userRepo = new Mock<IGenericRepository<BioUser>>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 5,
                IsApproved = true
            };
            deviceRepo.Setup(r => r.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            attendanceRepo.Setup(r => r.IsDuplicateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(false);
            userRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BioUser, bool>>>()))
                .ReturnsAsync(new List<BioUser>());  // no matching user

            BioAttendanceLog? capturedLog = null;
            attendanceRepo.Setup(r => r.AddAsync(It.IsAny<BioAttendanceLog>()))
                .Callback<BioAttendanceLog>(l => capturedLog = l)
                .Returns(Task.CompletedTask);

            uow.Setup(u => u.Devices).Returns(deviceRepo.Object);
            uow.Setup(u => u.Attendance).Returns(attendanceRepo.Object);
            uow.Setup(u => u.Users).Returns(userRepo.Object);
            uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var service = new AttendanceService(uow.Object);
            await service.ProcessPushAsync("SN001", new[]
            {
                new AttendanceRecordDto { UserCode = "UNKNOWN", PunchTime = DateTime.UtcNow }
            }, clientId: null);

            Assert.NotNull(capturedLog);
            Assert.Equal(string.Empty, capturedLog!.UserName);
        }
    }
}
