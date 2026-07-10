using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class IClockControllerTests
    {
        private IClockController BuildController(
            Mock<IDeviceService>? deviceSvcMock = null,
            Mock<IAttendanceService>? attendanceSvcMock = null,
            Mock<ICommandService>? commandSvcMock = null,
            Mock<IUserService>? userSvcMock = null)
        {
            deviceSvcMock ??= new Mock<IDeviceService>();
            attendanceSvcMock ??= new Mock<IAttendanceService>();
            commandSvcMock ??= new Mock<ICommandService>();
            userSvcMock ??= new Mock<IUserService>();
            var hubMock = new Mock<IHubContext<BiometricPushServer.Web.Hubs.AttendanceHub>>();
            var hubClientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();
            hubMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);
            hubClientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

            var loggerMock = new Mock<ILogger<IClockController>>();

            var controller = new IClockController(
                deviceSvcMock.Object,
                attendanceSvcMock.Object,
                commandSvcMock.Object,
                userSvcMock.Object,
                hubMock.Object,
                loggerMock.Object);

            return controller;
        }

        private static void SetRequestBody(ControllerBase controller, string body)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            var stream = new MemoryStream(bytes);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = bytes.Length;
            httpContext.Request.ContentType = "text/plain";
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        // ── ATTLOG parsing via CDataPost ─────────────────────────────────────

        [Fact]
        public async Task CDataPost_AttlogTable_ParsesTabSeparatedRecordsAndCallsService()
        {
            // Arrange
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 1,
                IsApproved = true
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            IEnumerable<AttendanceRecordDto>? capturedRecords = null;
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                    It.IsAny<int?>()))
                .Callback<string, IEnumerable<AttendanceRecordDto>, int?>((_, recs, __) =>
                    capturedRecords = recs)
                .ReturnsAsync((1, 0));
            attendanceSvcMock.Setup(s => s.GetTodayAsync(It.IsAny<int?>()))
                .ReturnsAsync(new List<AttendanceLogDto>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);

            // Tab-separated ATTLOG body: UserCode\tDateTime\tStatus\tVerifyMode\tWorkCode
            var body = "1\t2024-03-15 09:00:00\t0\t1\t0\r\n2\t2024-03-15 09:05:00\t1\t1\t0\r\n";
            SetRequestBody(controller, body);

            // Act
            var result = await controller.CDataPost("SN001", table: "ATTLOG");

            // Assert
            Assert.IsType<ContentResult>(result);
            var contentResult = (ContentResult)result;
            Assert.Equal("OK", contentResult.Content);

            Assert.NotNull(capturedRecords);
            var recordList = capturedRecords!.ToList();
            Assert.Equal(2, recordList.Count);
            Assert.Equal("1", recordList[0].UserCode);
            Assert.Equal(new DateTime(2024, 3, 15, 9, 0, 0), recordList[0].PunchTime);
            Assert.Equal(0, recordList[0].AttendanceState);
            Assert.Equal("2", recordList[1].UserCode);
            Assert.Equal(1, recordList[1].AttendanceState);
        }

        [Fact]
        public async Task CDataPost_AttlogTable_ParsesWhitespaceSeparatedRecordsAndCallsService()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN002",
                ClientId = 1,
                IsApproved = true
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN002")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            IEnumerable<AttendanceRecordDto>? capturedRecords = null;
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                    It.IsAny<int?>()))
                .Callback<string, IEnumerable<AttendanceRecordDto>, int?>((_, recs, __) =>
                    capturedRecords = recs)
                .ReturnsAsync((1, 0));
            attendanceSvcMock.Setup(s => s.GetTodayAsync(It.IsAny<int?>()))
                .ReturnsAsync(new List<AttendanceLogDto>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);

            // Variable spacing is intentional to verify parsing across devices that emit different whitespace widths.
            var body = "1    2024-03-15    09:00:00    0    1    0    0\r\n2    2024-03-15    09:05:00    1    2    9    0\r\n";
            SetRequestBody(controller, body);

            var result = await controller.CDataPost("SN002", table: "ATTLOG");

            Assert.IsType<ContentResult>(result);
            Assert.NotNull(capturedRecords);

            var recordList = capturedRecords!.ToList();
            Assert.Equal(2, recordList.Count);
            Assert.Equal("1", recordList[0].UserCode);
            Assert.Equal(new DateTime(2024, 3, 15, 9, 0, 0), recordList[0].PunchTime);
            Assert.Equal(0, recordList[0].AttendanceState);
            Assert.Equal(1, recordList[0].VerifyMode);
            Assert.Equal("0", recordList[0].WorkCode);

            Assert.Equal("2", recordList[1].UserCode);
            Assert.Equal(new DateTime(2024, 3, 15, 9, 5, 0), recordList[1].PunchTime);
            Assert.Equal(1, recordList[1].AttendanceState);
            Assert.Equal(2, recordList[1].VerifyMode);
            Assert.Equal("9", recordList[1].WorkCode);
        }

        [Fact]
        public async Task CDataPost_AttlogTable_SkipsBodyPreambleLine()
        {
            // Arrange — body starts with "table=ATTLOG" preamble (as some ZKTeco devices send)
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice { Id = 1, SerialNumber = "SN001", IsApproved = true };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            IEnumerable<AttendanceRecordDto>? capturedRecords = null;
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                    It.IsAny<int?>()))
                .Callback<string, IEnumerable<AttendanceRecordDto>, int?>((_, recs, __) =>
                    capturedRecords = recs)
                .ReturnsAsync((1, 0));
            attendanceSvcMock.Setup(s => s.GetTodayAsync(It.IsAny<int?>()))
                .ReturnsAsync(new List<AttendanceLogDto>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);

            // Body includes preamble "table=ATTLOG" before the records
            var body = "table=ATTLOG\r\n1\t2024-03-15 09:00:00\t0\t1\t0\r\n";
            SetRequestBody(controller, body);

            // Act
            await controller.CDataPost("SN001", table: "ATTLOG");

            // Assert — preamble line is skipped; only actual records are parsed
            Assert.NotNull(capturedRecords);
            Assert.Single(capturedRecords!);
        }

        [Fact]
        public async Task CDataPost_EmptyBody_ProcessPushCalledWithNoRecords()
        {
            // Arrange
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice { Id = 1, SerialNumber = "SN001", IsApproved = true };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            IEnumerable<AttendanceRecordDto>? capturedRecords = null;
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                    It.IsAny<int?>()))
                .Callback<string, IEnumerable<AttendanceRecordDto>, int?>((_, recs, __) =>
                    capturedRecords = recs)
                .ReturnsAsync((0, 0));

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);
            SetRequestBody(controller, string.Empty);

            // Act
            await controller.CDataPost("SN001", table: "ATTLOG");

            // Assert — ProcessPushAsync called but with zero records
            attendanceSvcMock.Verify(s => s.ProcessPushAsync(
                "SN001",
                It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                It.IsAny<int?>()), Times.Once);
            Assert.NotNull(capturedRecords);
            Assert.Empty(capturedRecords!);
        }

        [Fact]
        public async Task CDataPost_NonAttlogTable_DoesNotCallProcessPush()
        {
            // Arrange
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice { Id = 1, SerialNumber = "SN001", IsApproved = true };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN001")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);
            SetRequestBody(controller, "some data");

            // Act — table is OPERLOG, not ATTLOG
            await controller.CDataPost("SN001", table: "OPERLOG");

            // Assert — ProcessPushAsync should NOT be called
            attendanceSvcMock.Verify(s => s.ProcessPushAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task CDataPost_NewDevice_RegistersAndUsesReturnedDevice()
        {
            // Arrange — device is unknown (first-time registration)
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var registeredDevice = new BioDevice
            {
                Id = 99,
                SerialNumber = "SN_NEW",
                ClientId = 3,
                IsApproved = true
            };

            // First call returns null (device unknown); registration returns registered device
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_NEW"))
                .ReturnsAsync((BioDevice?)null);
            deviceSvcMock.Setup(s => s.RegisterOrUpdateAsync(
                    It.IsAny<DeviceRegistrationDto>(), It.IsAny<string>()))
                .ReturnsAsync(registeredDevice);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            int? capturedClientId = -1;
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<AttendanceRecordDto>>(),
                    It.IsAny<int?>()))
                .Callback<string, IEnumerable<AttendanceRecordDto>, int?>((_, __, cid) =>
                    capturedClientId = cid)
                .ReturnsAsync((1, 0));
            attendanceSvcMock.Setup(s => s.GetTodayAsync(It.IsAny<int?>()))
                .ReturnsAsync(new List<AttendanceLogDto>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);
            var body = "1\t2024-03-15 09:00:00\t0\t1\t0\r\n";
            SetRequestBody(controller, body);

            // Act
            await controller.CDataPost("SN_NEW", table: "ATTLOG");

            // Assert — clientId passed to ProcessPushAsync should come from the registered device
            Assert.Equal(3, capturedClientId);
        }

        // ── Route alias tests ────────────────────────────────────────────────

        [Fact]
        public void CDataPost_HasBothAspxAndPlainRoutes()
        {
            var methodInfo = typeof(IClockController).GetMethod(nameof(IClockController.CDataPost))!;
            var attrs = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false)
                .Cast<Microsoft.AspNetCore.Mvc.HttpPostAttribute>()
                .Select(a => a.Template)
                .ToList();

            Assert.Contains("cdata", attrs);
            Assert.Contains("cdata.aspx", attrs);
        }

        [Fact]
        public void CDataGet_HasBothAspxAndPlainRoutes()
        {
            var methodInfo = typeof(IClockController).GetMethod(nameof(IClockController.CDataGet))!;
            var attrs = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false)
                .Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>()
                .Select(a => a.Template)
                .ToList();

            Assert.Contains("cdata", attrs);
            Assert.Contains("cdata.aspx", attrs);
        }

        [Fact]
        public void GetRequest_HasBothAspxAndPlainRoutes()
        {
            var methodInfo = typeof(IClockController).GetMethod(nameof(IClockController.GetRequest))!;
            var attrs = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false)
                .Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>()
                .Select(a => a.Template)
                .ToList();

            Assert.Contains("getrequest", attrs);
            Assert.Contains("getrequest.aspx", attrs);
        }

        [Fact]
        public async Task CDataGet_PreviouslyOfflineApprovedDevice_QueuesAutomaticAttendanceSync()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();
            var commandSvcMock = new Mock<ICommandService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = true,
                IsActive = true,
                LastHeartbeatOn = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes - 1)
            };

            deviceSvcMock.Setup(s => s.RegisterOrUpdateAsync(It.IsAny<DeviceRegistrationDto>(), It.IsAny<string>()))
                .ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync("SN001", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            commandSvcMock.Setup(s => s.GetPendingAsync("SN001"))
                .ReturnsAsync(new List<BioDeviceCommand>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock, commandSvcMock);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await controller.CDataGet("SN001");

            Assert.IsType<ContentResult>(result);
            commandSvcMock.Verify(s => s.EnqueueAsync(
                "SN001",
                AppConstants.CommandSyncAttendanceLogs,
                null,
                7,
                null), Times.Once);
        }

        [Fact]
        public async Task CDataGet_ExistingPendingSync_DoesNotQueueDuplicateAutomaticSync()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();
            var commandSvcMock = new Mock<ICommandService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = true,
                IsActive = true,
                LastHeartbeatOn = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes - 1)
            };

            deviceSvcMock.Setup(s => s.RegisterOrUpdateAsync(It.IsAny<DeviceRegistrationDto>(), It.IsAny<string>()))
                .ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync("SN001", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            commandSvcMock.Setup(s => s.GetPendingAsync("SN001"))
                .ReturnsAsync(new[]
                {
                    new BioDeviceCommand
                    {
                        DeviceSN = "SN001",
                        CommandType = AppConstants.CommandSyncAttendanceLogs
                    }
                });

            var controller = BuildController(deviceSvcMock, attendanceSvcMock, commandSvcMock);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            await controller.CDataGet("SN001");

            commandSvcMock.Verify(s => s.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task CDataGet_RecentlyOnlineDevice_DoesNotQueueAutomaticSync()
        {
            await AssertAutomaticSyncQueuedAsync(new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = true,
                IsActive = true,
                LastHeartbeatOn = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes + 1)
            }, Times.Never());
        }

        [Fact]
        public async Task CDataGet_UnapprovedDevice_DoesNotQueueAutomaticSync()
        {
            await AssertAutomaticSyncQueuedAsync(new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = false,
                IsActive = true,
                LastHeartbeatOn = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes - 1)
            }, Times.Never());
        }

        [Fact]
        public async Task CDataGet_InactiveDevice_DoesNotQueueAutomaticSync()
        {
            await AssertAutomaticSyncQueuedAsync(new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = true,
                IsActive = false,
                LastHeartbeatOn = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes - 1)
            }, Times.Never());
        }

        [Fact]
        public async Task CDataGet_DeviceWithoutPreviousHeartbeat_DoesNotQueueAutomaticSync()
        {
            await AssertAutomaticSyncQueuedAsync(new BioDevice
            {
                Id = 1,
                SerialNumber = "SN001",
                ClientId = 7,
                IsApproved = true,
                IsActive = true,
                LastHeartbeatOn = null
            }, Times.Never());
        }

        [Fact]
        public async Task CDataPost_UserInfoTable_UpsertsParsedUsersAndAttachesThemToDevice()
        {
            // Arrange
            var deviceSvcMock = new Mock<IDeviceService>();
            var userSvcMock = new Mock<IUserService>();

            var device = new BioDevice
            {
                Id = 5,
                SerialNumber = "SN_U",
                ClientId = 2,
                IsApproved = true
            };

            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_U")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Return a BioUser for each upsert call
            userSvcMock.Setup(s => s.UpsertAsync(It.Is<UserDto>(u => u.UserCode == "1")))
                .ReturnsAsync(new BioUser { Id = 101, UserCode = "1" });
            userSvcMock.Setup(s => s.UpsertAsync(It.Is<UserDto>(u => u.UserCode == "2")))
                .ReturnsAsync(new BioUser { Id = 102, UserCode = "2" });
            userSvcMock.Setup(s => s.AttachUserToDeviceAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var controller = BuildController(deviceSvcMock, userSvcMock: userSvcMock);

            var body = "PIN=1\tName=Alice\tPri=0\tPasswd=\tCard=11111\tGrp=1\r\n" +
                       "PIN=2\tName=Bob\tPri=14\tPasswd=\tCard=22222\tGrp=1\r\n";
            SetRequestBody(controller, body);

            // Act
            var result = await controller.CDataPost("SN_U", table: "USERINFO");

            // Assert — both users upserted and mapped to device
            Assert.IsType<ContentResult>(result);
            Assert.Equal("OK", ((ContentResult)result).Content);

            userSvcMock.Verify(s => s.UpsertAsync(It.Is<UserDto>(u =>
                u.UserCode == "1" && u.Name == "Alice" && u.CardNumber == "11111" && u.ClientId == 2)),
                Times.Once);
            userSvcMock.Verify(s => s.UpsertAsync(It.Is<UserDto>(u =>
                u.UserCode == "2" && u.Name == "Bob" && u.Privilege == 14 && u.ClientId == 2)),
                Times.Once);
            userSvcMock.Verify(s => s.AttachUserToDeviceAsync(5, 101), Times.Once);
            userSvcMock.Verify(s => s.AttachUserToDeviceAsync(5, 102), Times.Once);
        }

        [Fact]
        public async Task CDataPost_UserInfoTable_SkipsPreambleLineAndInvalidLines()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var userSvcMock = new Mock<IUserService>();

            var device = new BioDevice { Id = 3, SerialNumber = "SN_U2", ClientId = 1, IsApproved = true };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_U2")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            userSvcMock.Setup(s => s.UpsertAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(new BioUser { Id = 200, UserCode = "42" });
            userSvcMock.Setup(s => s.AttachUserToDeviceAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var controller = BuildController(deviceSvcMock, userSvcMock: userSvcMock);

            // body has preamble and one line with no PIN
            var body = "table=USERINFO\r\nno-pin-field=something\r\nPIN=42\tName=Charlie\r\n";
            SetRequestBody(controller, body);

            await controller.CDataPost("SN_U2", table: "USERINFO");

            // Only the valid PIN=42 line should have triggered an upsert
            userSvcMock.Verify(s => s.UpsertAsync(It.IsAny<UserDto>()), Times.Once);
        }

        // ── Device secret validation ─────────────────────────────────────────

        [Fact]
        public async Task CDataPost_DeviceWithSecret_CorrectHeader_AllowsRequest()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                ClientId = 1,
                IsApproved = true,
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(It.IsAny<string>(), It.IsAny<IEnumerable<AttendanceRecordDto>>(), It.IsAny<int?>()))
                .ReturnsAsync((0, 0));

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[AppConstants.DeviceSecretHeader] = "mysecret";
            ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.CDataPost("SN_SEC", table: "ATTLOG");

            Assert.IsType<ContentResult>(result);
            Assert.Equal("OK", ((ContentResult)result).Content);
        }

        [Fact]
        public async Task CDataPost_DeviceWithSecret_WrongHeader_Returns401()
        {
            var deviceSvcMock = new Mock<IDeviceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);

            var controller = BuildController(deviceSvcMock);
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[AppConstants.DeviceSecretHeader] = "wrongsecret";
            ctx.Request.Body = new MemoryStream(Array.Empty<byte>());
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.CDataPost("SN_SEC", table: "ATTLOG");

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CDataPost_DeviceWithSecret_MissingHeader_Returns401()
        {
            var deviceSvcMock = new Mock<IDeviceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);

            var controller = BuildController(deviceSvcMock);
            var ctx = new DefaultHttpContext(); // no X-Device-Secret header
            ctx.Request.Body = new MemoryStream(Array.Empty<byte>());
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.CDataPost("SN_SEC", table: "ATTLOG");

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CDataPost_DeviceWithNoSecretConfigured_NoHeader_AllowsRequest()
        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_NOSEC",
                ClientId = 1,
                IsApproved = true,
                DeviceSecret = string.Empty // no secret
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_NOSEC")).ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            attendanceSvcMock.Setup(s => s.ProcessPushAsync(It.IsAny<string>(), It.IsAny<IEnumerable<AttendanceRecordDto>>(), It.IsAny<int?>()))
                .ReturnsAsync((0, 0));

            var controller = BuildController(deviceSvcMock, attendanceSvcMock);
            var ctx = new DefaultHttpContext(); // no header, no secret
            ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.CDataPost("SN_NOSEC", table: "ATTLOG");

            Assert.IsType<ContentResult>(result);
            Assert.Equal("OK", ((ContentResult)result).Content);
        }

        [Fact]
        public async Task CDataGet_DeviceWithSecret_WrongHeader_Returns401()
        {
            var deviceSvcMock = new Mock<IDeviceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);

            var controller = BuildController(deviceSvcMock);
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[AppConstants.DeviceSecretHeader] = "bad";
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.CDataGet("SN_SEC");

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetRequest_DeviceWithSecret_WrongHeader_Returns401()
        {
            var deviceSvcMock = new Mock<IDeviceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);

            var controller = BuildController(deviceSvcMock);
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[AppConstants.DeviceSecretHeader] = "bad";
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.GetRequest("SN_SEC");

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeviceCmd_DeviceWithSecret_WrongHeader_Returns401()
        {
            var deviceSvcMock = new Mock<IDeviceService>();

            var device = new BioDevice
            {
                Id = 1,
                SerialNumber = "SN_SEC",
                DeviceSecret = "mysecret"
            };
            deviceSvcMock.Setup(s => s.GetBySerialNumberAsync("SN_SEC")).ReturnsAsync(device);

            var controller = BuildController(deviceSvcMock);
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[AppConstants.DeviceSecretHeader] = "bad";
            ctx.Request.Body = new MemoryStream(Array.Empty<byte>());
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.DeviceCmd("SN_SEC");

            Assert.IsType<UnauthorizedResult>(result);
        }

        private async Task AssertAutomaticSyncQueuedAsync(BioDevice device, Times expectedTimes)        {
            var deviceSvcMock = new Mock<IDeviceService>();
            var attendanceSvcMock = new Mock<IAttendanceService>();
            var commandSvcMock = new Mock<ICommandService>();

            deviceSvcMock.Setup(s => s.RegisterOrUpdateAsync(It.IsAny<DeviceRegistrationDto>(), It.IsAny<string>()))
                .ReturnsAsync(device);
            deviceSvcMock.Setup(s => s.UpdateHeartbeatAsync(device.SerialNumber, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            commandSvcMock.Setup(s => s.GetPendingAsync(device.SerialNumber))
                .ReturnsAsync(new List<BioDeviceCommand>());

            var controller = BuildController(deviceSvcMock, attendanceSvcMock, commandSvcMock);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            await controller.CDataGet(device.SerialNumber);

            commandSvcMock.Verify(s => s.EnqueueAsync(
                device.SerialNumber,
                AppConstants.CommandSyncAttendanceLogs,
                null,
                device.ClientId,
                null), expectedTimes);
        }
    }
}
