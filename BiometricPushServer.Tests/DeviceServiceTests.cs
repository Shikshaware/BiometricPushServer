using System;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class DeviceServiceTests
    {
        private static IConfiguration BuildConfig(bool autoApprove)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string?>
            {
                ["DeviceCompatibility:AutoApproveDevices"] = autoApprove.ToString()
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }

        private static DeviceService BuildSut(Mock<IUnitOfWork> uowMock, bool autoApprove = true)
            => new DeviceService(uowMock.Object, BuildConfig(autoApprove));

        [Fact]
        public async Task RegisterOrUpdateAsync_AutoApproveTrue_NewDeviceIsApproved()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var deviceRepoMock = new Mock<IDeviceRepository>();
            uowMock.Setup(u => u.Devices).Returns(deviceRepoMock.Object);
            deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN001"))
                .ReturnsAsync((BioDevice?)null);
            deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioDevice>())).Returns(Task.CompletedTask);
            uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            BioDevice? capturedDevice = null;
            deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioDevice>()))
                .Callback<BioDevice>(d => capturedDevice = d)
                .Returns(Task.CompletedTask);

            var sut = BuildSut(uowMock, autoApprove: true);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN001", DeviceName = "Device 1" };
            await sut.RegisterOrUpdateAsync(dto, "192.168.1.1");

            // Assert — IsApproved must be true when AutoApproveDevices = true
            Assert.NotNull(capturedDevice);
            Assert.True(capturedDevice!.IsApproved,
                "New device should be auto-approved when AutoApproveDevices=true");
        }

        [Fact]
        public async Task RegisterOrUpdateAsync_AutoApproveFalse_NewDeviceNotApproved()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var deviceRepoMock = new Mock<IDeviceRepository>();
            uowMock.Setup(u => u.Devices).Returns(deviceRepoMock.Object);
            deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN002"))
                .ReturnsAsync((BioDevice?)null);
            uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            BioDevice? capturedDevice = null;
            deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioDevice>()))
                .Callback<BioDevice>(d => capturedDevice = d)
                .Returns(Task.CompletedTask);

            var sut = BuildSut(uowMock, autoApprove: false);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN002", DeviceName = "Device 2" };
            await sut.RegisterOrUpdateAsync(dto, "10.0.0.1");

            // Assert — IsApproved must be false when AutoApproveDevices=false
            Assert.NotNull(capturedDevice);
            Assert.False(capturedDevice!.IsApproved,
                "New device should NOT be auto-approved when AutoApproveDevices=false");
        }

        [Fact]
        public async Task RegisterOrUpdateAsync_ExistingDevice_DoesNotChangeApproval()
        {
            // Arrange
            var uowMock = new Mock<IUnitOfWork>();
            var deviceRepoMock = new Mock<IDeviceRepository>();
            uowMock.Setup(u => u.Devices).Returns(deviceRepoMock.Object);
            uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var existingDevice = new BioDevice
            {
                Id = 10,
                SerialNumber = "SN003",
                DeviceName = "Existing",
                IsApproved = true   // already approved
            };
            deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN003"))
                .ReturnsAsync(existingDevice);
            deviceRepoMock.Setup(r => r.Update(It.IsAny<BioDevice>()));

            var sut = BuildSut(uowMock, autoApprove: false);  // auto-approve off

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN003", DeviceName = "Existing" };
            var result = await sut.RegisterOrUpdateAsync(dto, "10.0.0.2");

            // Assert — existing approval state preserved
            Assert.True(result!.IsApproved,
                "Existing approved device should retain IsApproved=true after update");
            deviceRepoMock.Verify(r => r.AddAsync(It.IsAny<BioDevice>()), Times.Never);
        }

        [Fact]
        public async Task RegisterOrUpdateAsync_DefaultConfig_AutoApproves()
        {
            // Arrange — no AutoApproveDevices key in config (default should be true)
            var uowMock = new Mock<IUnitOfWork>();
            var deviceRepoMock = new Mock<IDeviceRepository>();
            uowMock.Setup(u => u.Devices).Returns(deviceRepoMock.Object);
            deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN004"))
                .ReturnsAsync((BioDevice?)null);
            uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            BioDevice? capturedDevice = null;
            deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<BioDevice>()))
                .Callback<BioDevice>(d => capturedDevice = d)
                .Returns(Task.CompletedTask);

            // Empty config — key not present
            var emptyConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>())
                .Build();
            var sut = new DeviceService(uowMock.Object, emptyConfig);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN004", DeviceName = "Default" };
            await sut.RegisterOrUpdateAsync(dto, "10.0.0.3");

            // Assert — defaults to auto-approve when key is absent
            Assert.NotNull(capturedDevice);
            Assert.True(capturedDevice!.IsApproved,
                "Should auto-approve by default when AutoApproveDevices config key is absent");
        }
    }
}
