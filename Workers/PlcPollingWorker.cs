using System;
using System.Threading;
using System.Threading.Tasks;
using GuanHeBridgeMonitor.Hubs;
using GuanHeBridgeMonitor.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GuanHeBridgeMonitor.Workers
{
    public class PlcPollingWorker : BackgroundService
    {
        private readonly ILogger<PlcPollingWorker> _logger;
        private readonly IPlcService _plc;
        private readonly IBmsService _bms;
        private readonly IHubContext<BridgeMonitorHub> _hub;
        private readonly int _intervalMs;

        public PlcPollingWorker(
            ILogger<PlcPollingWorker> logger,
            IConfiguration cfg,
            IPlcService plc,
            IBmsService bms,
            IHubContext<BridgeMonitorHub> hub)
        {
            _logger = logger;
            _plc = plc;
            _bms = bms;
            _hub = hub;
            _intervalMs = Math.Max(500, cfg.GetValue("Polling:IntervalMs", 1000));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PlcPollingWorker started. Interval={Interval}ms", _intervalMs);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var vs = await _plc.GetVehicleStatusAsync(1);
                    var bs = await _bms.GetBatteryStatusAsync(1);

                    await _hub.Clients.All.SendAsync("ReceiveVehicleStatus", vs, stoppingToken);
                    await _hub.Clients.All.SendAsync("ReceiveBatteryStatus", bs, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PlcPollingWorker loop error");
                }

                try { await Task.Delay(_intervalMs, stoppingToken); }
                catch { }
            }

            _logger.LogInformation("PlcPollingWorker stopped.");
        }
    }
}
