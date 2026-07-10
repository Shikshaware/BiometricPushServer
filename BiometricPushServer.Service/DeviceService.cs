using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;

namespace BiometricPushServer.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly IUnitOfWork _uow;

        public DeviceService(IUnitOfWork uow)
        {
            _uow = uow;
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
                    IsApproved = false,
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

        public async Task UpdateConnectionAsync(string sn, string ipAddress)
        {
            var device = await _uow.Devices.GetBySerialNumberAsync(sn);
            if (device == null) return;
            device.IpAddress = ipAddress;
            device.LastConnectedOn = DateTime.UtcNow;
            device.UpdatedOn = DateTime.UtcNow;
            _uow.Devices.Update(device);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<DeviceDto>> GetAllDevicesAsync(int? clientId = null, int? locationId = null)
        {
            var devices = await _uow.Devices.FindAsync(d =>
                (clientId == null || d.ClientId == clientId) &&
                (locationId == null || d.LocationId == locationId));
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
            if (!device.ClientId.HasValue || !device.LocationId.HasValue) return false;
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
            if (dto.LocationId.HasValue)
            {
                var location = await _uow.Locations.GetByIdAsync(dto.LocationId.Value);
                if (location == null) return null;
                if (device.ClientId.HasValue && location.ClientId.HasValue && device.ClientId != location.ClientId)
                    return null;

                device.LocationId = dto.LocationId;
                device.ClientId ??= location.ClientId;
            }
            device.UpdatedOn = DateTime.UtcNow;

            _uow.Devices.Update(device);
            await _uow.SaveChangesAsync();

            var onlineThreshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            return MapToDto(device, onlineThreshold);
        }

        public async Task<int> BulkAssignLocationAsync(IEnumerable<int> deviceIds, int locationId, int? clientId = null)
        {
            var location = await _uow.Locations.GetByIdAsync(locationId);
            if (location == null) return 0;

            var ids = deviceIds.Distinct().ToList();
            if (ids.Count == 0) return 0;

            var devices = await _uow.Devices.FindAsync(d =>
                ids.Contains(d.Id) &&
                (clientId == null || d.ClientId == clientId));

            var updated = 0;
            foreach (var device in devices)
            {
                if (location.ClientId.HasValue &&
                    device.ClientId.HasValue &&
                    device.ClientId != location.ClientId)
                {
                    continue;
                }

                device.LocationId = locationId;
                device.ClientId ??= location.ClientId;
                device.UpdatedOn = DateTime.UtcNow;
                _uow.Devices.Update(device);
                updated++;
            }

            if (updated > 0)
                await _uow.SaveChangesAsync();

            return updated;
        }

        private static DeviceDto MapToDto(BioDevice d, DateTime onlineThreshold) => new DeviceDto
        {
            Id = d.Id,
            ClientId = d.ClientId,
            SerialNumber = d.SerialNumber,
            DeviceName = d.DeviceName,
            IpAddress = d.IpAddress,
            FirmwareVersion = d.FirmwareVersion,
            Location = d.Location,
            LocationId = d.LocationId,
            IsApproved = d.IsApproved,
            IsActive = d.IsActive,
            IsLocked = d.IsLocked,
            IsOnline = d.LastHeartbeatOn >= onlineThreshold,
            LastConnectedOn = d.LastConnectedOn,
            LastHeartbeatOn = d.LastHeartbeatOn
        };
    }
}
