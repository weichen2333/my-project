using System.Threading.Tasks;
using GuanHeBridgeMonitor.Models;

namespace GuanHeBridgeMonitor.Services.Interfaces
{
    public interface IBmsService
    {
        Task<BatteryStatus> GetBatteryStatusAsync(int vehicleId);
    }
}
