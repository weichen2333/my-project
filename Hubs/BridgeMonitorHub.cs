using GuanHeBridgeMonitor.Models;
using GuanHeBridgeMonitor.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GuanHeBridgeMonitor.Hubs
{
    public class BridgeMonitorHub : Hub
    {
        private readonly IPlcService _plc;
        private readonly IBmsService _bms;
        private readonly ILogger<BridgeMonitorHub> _logger;

        public BridgeMonitorHub(IPlcService plc, IBmsService bms, ILogger<BridgeMonitorHub> logger)
        {
            _plc = plc; _bms = bms; _logger = logger;
        }

        public async Task SendVehicleCommand(int vehicleId, string command)
        {
            _logger.LogInformation("SignalR: Web命令 {cmd} -> 车辆 {id}", command, vehicleId);
            try
            {
                var ok = await _plc.SendVehicleControlCommand(vehicleId, command);

                if (ok)
                {
                    // 回执（成功）
                    await Clients.Caller.SendAsync("CommandResult", new
                    {
                        vehicleId,
                        command,
                        success = true,
                        message = $"已执行：{command}"
                    });

                    // 成功后单播一次最新状态（让用户能马上看到变化）
                    var vs = await _plc.GetVehicleStatusAsync(vehicleId);
                    var bs = await _bms.GetBatteryStatusAsync(vehicleId);
                    await Clients.Caller.SendAsync("ReceiveVehicleStatus", vs);
                    await Clients.Caller.SendAsync("ReceiveBatteryStatus", bs);
                }
                else
                {
                    await Clients.Caller.SendAsync("CommandResult", new
                    {
                        vehicleId,
                        command,
                        success = false,
                        message = $"PLC 未确认：{command}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行命令 {cmd} 异常", command);
                await Clients.Caller.SendAsync("CommandResult", new
                {
                    vehicleId,
                    command,
                    success = false,
                    message = $"执行异常：{ex.Message}"
                });
            }
        }

        public async Task RequestSnapshot(int vehicleId = 1)
        {
            try
            {
                var vs = await _plc.GetVehicleStatusAsync(vehicleId);
                var bs = await _bms.GetBatteryStatusAsync(vehicleId);
                await Clients.Caller.SendAsync("ReceiveVehicleStatus", vs);
                await Clients.Caller.SendAsync("ReceiveBatteryStatus", bs);

                await Clients.Caller.SendAsync("CommandResult", new
                {
                    vehicleId,
                    command = "RequestSnapshot",
                    success = true,
                    message = "已刷新最新状态"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RequestSnapshot 失败");
                await Clients.Caller.SendAsync("CommandResult", new
                {
                    vehicleId,
                    command = "RequestSnapshot",
                    success = false,
                    message = "刷新失败：" + ex.Message
                });
            }
        }
    }
}
