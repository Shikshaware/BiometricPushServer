using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BiometricPushServer.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly IUnitOfWork _uow;
        private readonly bool _autoApproveDevices;

        public DeviceService(IUnitOfWork uow, IConfiguration configuration)
        {
            _uow = uow;
            _autoApproveDevices = !string.Equals(
                configuration["DeviceCompatibility:AutoApproveDevices"],
                "false",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<BioDevice?> GetBySerialNumberAsync(string sn) =>
            await _uow.Devices.GetBySerialNumberAsync(sn);

        public async Task<BioDevice?> RegisterOrUpdateAsync(DeviceRegistrationDto dto, string ipAddress)
        {
            var device = await _uow.Devices.GetBySerialNumberAsync(dto.SerialNumber);

            if (device == null)
            {
                device = new BioDevice
                {
                    SerialNumber = dto.SerialNumber,
                    DeviceName = dto.DeviceName,
                    IpAddress = ipAddress,
                    Port = dto.Port,
                    FirmwareVersion = dto.FirmwareVersion,
                    DeviceModel = dto.DeviceModel,
                    ClientId = dto.ClientId,
                    IsActive = true,
                    IsApproved = _autoApproveDevices,
                    CreatedOn = DateTime.UtcNow
                };
                await _uow.Devices.AddAsync(device);
            }
            else
            {
                device.IpAddress = ipAddress;
                device.FirmwareVersion = dto.FirmwareVersion;
                device.LastConnectedOn = DateTime.UtcNow;
                _uow.Devices.Update(device);
            }

            await _uow.SaveChangesAsync();
            return device;
        }

        public async Task<IEnumerable<DeviceDto>> GetAllDevicesAsync(int? clientId = null)
        {
            var devices = await _uow.Devices.FindAsync(d => clientId == null || d.ClientId == clientId);
            var onlineThreshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            return devices.Select(d => MapToDto(d, onlineThreshold));
        }

        public async Task<DeviceDto?> GetDeviceDtoAsync(int deviceId)
        {
            var device = await _uow.Devices.GetByIdAsync(deviceId);
            if (device == null) return null;
            var onlineThreshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            return MapToDto(device, onlineThreshold);
        }

        public async Task<bool> ApproveDeviceAsync(int deviceId)
        {
            var device = await _uow.Devices.GetByIdAsync(deviceId);
            if (device == null) return false;
            device.IsApproved = true;
            _uow.Devices.Update(device);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetLockedAsync(int deviceId, bool locked)
        {
            var device = await _uow.Devices.GetByIdAsync(deviceId);
            if (device == null) return false;
            device.IsLocked = locked;
            _uow.Devices.Update(device);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task UpdateHeartbeatAsync(string sn, string ipAddress, string rawQuery)
        {
            var device = await _uow.Devices.GetBySerialNumberAsync(sn);
            if (device != null)
            {
                device.LastHeartbeatOn = DateTime.UtcNow;
                device.IpAddress = ipAddress;
                _uow.Devices.Update(device);
            }

            await _uow.Heartbeats.AddAsync(new BioHeartbeat
            {
                DeviceId = device?.Id,
                DeviceSN = sn,
                ClientId = device?.ClientId,
                IpAddress = ipAddress,
                PingTime = DateTime.UtcNow,
                RawQuery = rawQuery
            });

            await _uow.SaveChangesAsync();
        }

        public async Task MarkOfflineDevicesAsync()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            var staleDevices = await _uow.Devices.FindAsync(d =>
                d.IsActive && d.LastHeartbeatOn < threshold);

            foreach (var device in staleDevices)
            {
                await _uow.DeviceStatuses.AddAsync(new BioDeviceStatus
                {
                    DeviceId = device.Id,
                    DeviceSN = device.SerialNumber,
                    ClientId = device.ClientId,
                    IsOnline = false,
                    IpAddress = device.IpAddress,
                    StatusTime = DateTime.UtcNow,
                    Reason = "TIMEOUT"
                });
            }

            if (staleDevices.Any())
                await _uow.SaveChangesAsync();
        }

        public async Task<DeviceDto?> UpdateDeviceAsync(int deviceId, DeviceUpdateDto dto)
        {
            var device = await _uow.Devices.GetByIdAsync(deviceId);
            if (device == null) return null;

            device.DeviceName = dto.DeviceName;
            device.Location = dto.Location;
            if (dto.ClientId.HasValue) device.ClientId = dto.ClientId;
            if (dto.LocationId.HasValue) device.LocationId = dto.LocationId;
            device.UpdatedOn = DateTime.UtcNow;

            _uow.Devices.Update(device);
            await _uow.SaveChangesAsync();

            var onlineThreshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            return MapToDto(device, onlineThreshold);
        }

        private static DeviceDto MapToDto(BioDevice d, DateTime onlineThreshold) => new DeviceDto
        {
            Id = d.Id,
            SerialNumber = d.SerialNumber,
            DeviceName = d.DeviceName,
            IpAddress = d.IpAddress,
            FirmwareVersion = d.FirmwareVersion,
            Location = d.Location,
            IsApproved = d.IsApproved,
            IsActive = d.IsActive,
            IsOnline = d.LastHeartbeatOn >= onlineThreshold,
            LastConnectedOn = d.LastConnectedOn,
            LastHeartbeatOn = d.LastHeartbeatOn
        };
    }
}
