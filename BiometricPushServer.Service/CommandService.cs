using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;

namespace BiometricPushServer.Service
{
    public class CommandService : ICommandService
    {
        private readonly IUnitOfWork _uow;

        public CommandService(IUnitOfWork uow) => _uow = uow;

        public async Task<BioDeviceCommand> EnqueueAsync(
            string deviceSN, string commandType, string? parameters = null, int? clientId = null, string? commandText = null)
        {
            var device = await _uow.Devices.GetBySerialNumberAsync(deviceSN);

            var cmd = new BioDeviceCommand
            {
                DeviceSN = deviceSN,
                DeviceId = device?.Id,
                ClientId = clientId ?? device?.ClientId,
                CommandType = commandType,
                CommandText = string.IsNullOrWhiteSpace(commandText) ? commandType : commandText,
                Parameters = parameters ?? string.Empty,
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddMinutes(AppConstants.DefaultCommandTimeoutMinutes)
            };

            await _uow.Commands.AddAsync(cmd);
            await _uow.SaveChangesAsync();
            return cmd;
        }

        public async Task<IEnumerable<BioDeviceCommand>> GetPendingAsync(string deviceSN) =>
            await _uow.Commands.GetPendingCommandsAsync(deviceSN);

        public async Task<IEnumerable<BioDeviceCommand>> GetAllPendingAsync() =>
            await _uow.Commands.GetAllPendingAsync();

        public async Task<IEnumerable<BioDeviceCommand>> GetSentExpiredAsync() =>
            await _uow.Commands.GetSentExpiredAsync();

        public async Task MarkSentAsync(int commandId)
        {
            var cmd = await _uow.Commands.GetByIdAsync(commandId);
            if (cmd == null) return;
            cmd.IsSent = true;
            cmd.SentOn = DateTime.UtcNow;
            _uow.Commands.Update(cmd);
            await _uow.SaveChangesAsync();
        }

        public async Task MarkExecutedAsync(int commandId, string response)
        {
            var cmd = await _uow.Commands.GetByIdAsync(commandId);
            if (cmd == null) return;
            cmd.IsExecuted = true;
            cmd.ExecutedOn = DateTime.UtcNow;
            cmd.ResponseText = response;
            _uow.Commands.Update(cmd);
            await _uow.SaveChangesAsync();
        }

        public async Task MarkFailedAsync(int commandId, string reason)
        {
            var cmd = await _uow.Commands.GetByIdAsync(commandId);
            if (cmd == null) return;
            cmd.IsFailed = true;
            cmd.ResponseText = reason;
            cmd.RetryCount++;
            _uow.Commands.Update(cmd);
            await _uow.SaveChangesAsync();
        }
    }
}
