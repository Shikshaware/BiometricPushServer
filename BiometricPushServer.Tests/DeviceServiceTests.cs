using System;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class DeviceServiceTests
    {
        private static DeviceService BuildSut(Mock<IUnitOfWork> uowMock)
            => new DeviceService(uowMock.Object);

        [Fact]
        public async Task RegisterOrUpdateAsync_NewDeviceIsNotApproved()
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

            var sut = BuildSut(uowMock);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN001", DeviceName = "Device 1" };
            await sut.RegisterOrUpdateAsync(dto, "192.168.1.1");

            // Assert — new devices require explicit admin approval
            Assert.NotNull(capturedDevice);
            Assert.False(capturedDevice!.IsApproved,
                "New device should not be auto-approved");
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

            var sut = BuildSut(uowMock);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN003", DeviceName = "Existing" };
            var result = await sut.RegisterOrUpdateAsync(dto, "10.0.0.2");

            // Assert — existing approval state preserved
            Assert.True(result!.IsApproved,
                "Existing approved device should retain IsApproved=true after update");
            deviceRepoMock.Verify(r => r.AddAsync(It.IsAny<BioDevice>()), Times.Never);
        }

        [Fact]
        public async Task RegisterOrUpdateAsync_ConfigKeyPresence_DoesNotAutoApprove()
        {
            // Arrange
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

            var sut = BuildSut(uowMock);

            // Act
            var dto = new DeviceRegistrationDto { SerialNumber = "SN004", DeviceName = "Default" };
            await sut.RegisterOrUpdateAsync(dto, "10.0.0.3");

            // Assert — config key no longer influences registration approval
            Assert.NotNull(capturedDevice);
            Assert.False(capturedDevice!.IsApproved,
                "Device should remain unapproved until an admin approves it");
        }
    }
}
