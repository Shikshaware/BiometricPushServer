using System.Collections.Generic;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class UserControllerTests
    {
        private static UserController BuildController(
            Mock<IUserService> userService,
            Mock<IDeviceService> deviceService,
            Mock<ICommandService> commandService)
        {
            var httpContext = new DefaultHttpContext();
            var controller = new UserController(userService.Object, deviceService.Object, commandService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(
                    httpContext,
                    Mock.Of<ITempDataProvider>())
            };
            return controller;
        }

        [Fact]
        public async Task Index_UsesSelectedDeviceScopedUsers()
        {
            var userService = new Mock<IUserService>();
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetAllDevicesAsync(null)).ReturnsAsync(new List<DeviceDto>
            {
                new DeviceDto { Id = 1, SerialNumber = "SN1" },
                new DeviceDto { Id = 2, SerialNumber = "SN2" }
            });

            userService.Setup(s => s.GetByDeviceAsync(2, 1, 50)).ReturnsAsync(new PagedResult<UserDto>
            {
                Items = new List<UserDto> { new UserDto { UserCode = "u2", Name = "User 2" } },
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 50
            });

            var controller = BuildController(userService, deviceService, commandService);
            var result = await controller.Index(2, null, null, 1, 50);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PagedResult<UserDto>>(view.Model);
            Assert.Single(model.Items);
            Assert.Equal("u2", model.Items[0].UserCode);

            userService.Verify(s => s.GetByDeviceAsync(2, 1, 50), Times.Once);
            userService.Verify(s => s.GetAllAsync(It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Upsert_EnsuresUserIsMappedToSelectedDevice()
        {
            var userService = new Mock<IUserService>();
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetDeviceDtoAsync(5)).ReturnsAsync(new DeviceDto
            {
                Id = 5,
                ClientId = 10,
                SerialNumber = "SN5"
            });

            userService.Setup(s => s.UpsertAsync(It.IsAny<UserDto>())).ReturnsAsync(new BioUser
            {
                Id = 101,
                UserCode = "u101",
                Name = "User 101"
            });

            commandService.Setup(s => s.EnqueueAsync(
                    "SN5",
                    "DATA UPDATE USERINFO",
                    null,
                    null,
                    It.IsAny<string>()))
                .ReturnsAsync(new BioDeviceCommand());

            var controller = BuildController(userService, deviceService, commandService);
            await controller.Upsert(5, new UserDto { UserCode = "u101", Name = "User 101" });

            userService.Verify(s => s.AttachUserToDeviceAsync(5, 101), Times.Once);
        }

        [Fact]
        public async Task Delete_DetachesUserFromSelectedDevice()
        {
            var userService = new Mock<IUserService>();
            var deviceService = new Mock<IDeviceService>();
            var commandService = new Mock<ICommandService>();

            deviceService.Setup(s => s.GetDeviceDtoAsync(9)).ReturnsAsync(new DeviceDto
            {
                Id = 9,
                ClientId = 1,
                SerialNumber = "SN9"
            });

            userService.Setup(s => s.GetByCodeAsync("u9", 1)).ReturnsAsync(new BioUser
            {
                Id = 900,
                UserCode = "u9"
            });
            userService.Setup(s => s.DetachUserFromDeviceAsync(9, 900)).ReturnsAsync(true);
            userService.Setup(s => s.UserHasAnyDeviceMappingAsync(900)).ReturnsAsync(false);
            userService.Setup(s => s.DeleteAsync("u9", 1)).ReturnsAsync(true);

            commandService.Setup(s => s.EnqueueAsync(
                    "SN9",
                    "DATA DELETE USERINFO",
                    null,
                    null,
                    It.IsAny<string>()))
                .ReturnsAsync(new BioDeviceCommand());

            var controller = BuildController(userService, deviceService, commandService);
            var result = await controller.Delete(9, "u9");

            Assert.IsType<RedirectToActionResult>(result);
            userService.Verify(s => s.DetachUserFromDeviceAsync(9, 900), Times.Once);
        }
    }
}
