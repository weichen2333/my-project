using System.Collections.Generic;
using System.Threading.Tasks;
using GuanHeBridgeMonitor.Models;

namespace GuanHeBridgeMonitor.Services.Interfaces
{
    public interface IPlcService
    {
        // 业务命令：启动/停止/前进/后退（我们在 RealPlcService 里做了写入+ACK）
        Task<bool> SendVehicleControlCommand(int vehicleId, string command);

        // 底层直写（测试/高级操作）
        Task<bool> SendCommandAsync(string register, int value);

        // 读取寄存器
        Task<int?> ReadIntRegisterAsync(string register);   // D / 其它字寄存器
        Task<bool?> ReadBitRegisterAsync(string register);   // M / X / Y 等位寄存器

        // 单车状态（车辆 + 电池）
        Task<VehicleStatus> GetVehicleStatusAsync(int vehicleId);
        Task<BatteryStatus> GetBatteryStatusAsync(int vehicleId);

        // 汇总/列表（用于总览/摄像头信息推送的调用方）
        Task<IEnumerable<VehicleOverview>> GetAllVehicleOverviewsAsync();
        Task<IEnumerable<CameraInfo>> GetCameraInfosAsync();
    }
}
