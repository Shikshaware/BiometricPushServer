using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class AttendanceApiControllerTests
    {
        private static AttendanceApiController BuildController(
            Mock<IAttendanceService> attendanceService,
            Mock<IDeviceService> deviceService,
            ClaimsPrincipal? user = null)
        {
            var controller = new AttendanceApiController(attendanceService.Object, deviceService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                    }
                }
            };

            return controller;
        }

        [Fact]
        public async Task GetByUser_UsesClaimScopedClientId()
        {
            var attendanceService = new Mock<IAttendanceService>();
            var deviceService = new Mock<IDeviceService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(AppConstants.Claim_ClientId, "7")
            }, "TestAuth"));

            attendanceService
                .Setup(s => s.GetByUserAsync("U1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), 7))
                .ReturnsAsync(new List<AttendanceLogDto>());

            var controller = BuildController(attendanceService, deviceService, user);
            var result = await controller.GetByUser("U1", clientId: 99, from: null, to: null);

            Assert.IsType<OkObjectResult>(result);
            attendanceService.Verify(
                s => s.GetByUserAsync("U1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), 7),
                Times.Once);
            attendanceService.Verify(
                s => s.GetByUserAsync("U1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), 99),
                Times.Never);
        }

        [Fact]
        public async Task GetReport_ClientUserCannotEscapeClientScope()
        {
            var attendanceService = new Mock<IAttendanceService>();
            var deviceService = new Mock<IDeviceService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(AppConstants.Claim_ClientId, "5")
            }, "TestAuth"));

            attendanceService
                .Setup(s => s.GetClientAttendanceReportAsync(5, AttendanceReportPeriod.Daily, It.IsAny<DateTime?>(), null))
                .ReturnsAsync(new AttendanceReportDto { ClientId = 5, Period = "daily" });

            var controller = BuildController(attendanceService, deviceService, user);
            var result = await controller.GetReport("daily", clientId: 42, referenceDate: null, locationId: null);

            Assert.IsType<OkObjectResult>(result);
            attendanceService.Verify(
                s => s.GetClientAttendanceReportAsync(5, AttendanceReportPeriod.Daily, It.IsAny<DateTime?>(), null),
                Times.Once);
            attendanceService.Verify(
                s => s.GetClientAttendanceReportAsync(42, AttendanceReportPeriod.Daily, It.IsAny<DateTime?>(), null),
                Times.Never);
        }

        [Fact]
        public async Task GetReport_ProviderWithoutClientId_ReturnsBadRequest()
        {
            var attendanceService = new Mock<IAttendanceService>();
            var deviceService = new Mock<IDeviceService>();
            var controller = BuildController(attendanceService, deviceService);

            var result = await controller.GetReport("daily", clientId: null, referenceDate: null, locationId: null);

            Assert.IsType<BadRequestObjectResult>(result);
            attendanceService.Verify(
                s => s.GetClientAttendanceReportAsync(It.IsAny<int>(), It.IsAny<AttendanceReportPeriod>(), It.IsAny<DateTime?>(), It.IsAny<int?>()),
                Times.Never);
        }
    }
}
