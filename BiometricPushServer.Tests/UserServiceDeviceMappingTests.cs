using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using Moq;
using Xunit;

namespace BiometricPushServer.Tests
{
    public class UserServiceDeviceMappingTests
    {
        [Fact]
        public async Task AttachUserToDeviceAsync_IsIdempotent()
        {
            var uow = new Mock<IUnitOfWork>();
            var userRepo = new Mock<IGenericRepository<BioUser>>();
            var mapRepo = new Mock<IGenericRepository<BioDeviceUserMap>>();

            mapRepo.SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<BioDeviceUserMap, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            uow.SetupGet(x => x.Users).Returns(userRepo.Object);
            uow.SetupGet(x => x.DeviceUserMaps).Returns(mapRepo.Object);
            uow.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var service = new UserService(uow.Object);

            await service.AttachUserToDeviceAsync(7, 11);
            await service.AttachUserToDeviceAsync(7, 11);

            mapRepo.Verify(r => r.AddAsync(It.IsAny<BioDeviceUserMap>()), Times.Once);
            uow.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
