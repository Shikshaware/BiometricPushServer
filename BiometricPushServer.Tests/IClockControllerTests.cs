using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Mock<IAttendanceService>? attendanceSvcMock = null)
        {
            deviceSvcMock ??= new Mock<IDeviceService>();
            attendanceSvcMock ??= new Mock<IAttendanceService>();

            var commandSvcMock = new Mock<ICommandService>();
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
    }
}
