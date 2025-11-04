using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GuanHeBridgeMonitor.Models;
using GuanHeBridgeMonitor.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuanHeBridgeMonitor.Services.Hardware
{
    /// <summary>
    /// 真实 PLC 服务实现：
    /// - 业务命令（启动/停止/前进/后退）带 ACK（写入后轮询同一位直到确认或超时）
    /// - 车辆&电池(BMS)状态均来自 PLC；当前阶段所有字段临时统一读取 D300
    /// - 兼容现有项目：实现 IPlcService 与 IBmsService 的全部方法
    /// </summary>
    public class RealPlcService : IPlcService, IBmsService
    {
        private readonly ILogger<RealPlcService> _logger;

        // TODO：把这里换成你的 PLC SDK 连接对象（例如三菱 ActUtlType）
        // private readonly ActUtlType _plc;

        public RealPlcService(ILogger<RealPlcService> logger)
        {
            _logger = logger;
            // _plc = new ActUtlType();
            // _plc.ActLogicalStationNumber = 1; // 举例
            // var rc = _plc.Open();
            // if (rc != 0) { throw new InvalidOperationException($"PLC 打开失败 rc={rc}"); }
        }

        #region 业务命令：启动/停止/前进/后退（带 ACK）
        public async Task<bool> SendVehicleControlCommand(int vehicleId, string command)
        {
            // 你现场的寄存器映射（位寄存器）
            const string startStopRegister = "M3000"; // 启动/停止
            const string fwdRevRegister = "M3002"; // 前进/后退

            string? targetReg = null;
            int targetVal = 0;

            switch (command?.Trim())
            {
                case "Start": targetReg = startStopRegister; targetVal = 1; break;
                case "Stop": targetReg = startStopRegister; targetVal = 0; break;
                case "Forward": targetReg = fwdRevRegister; targetVal = 1; break;
                case "Reverse": targetReg = fwdRevRegister; targetVal = 0; break;
                default:
                    _logger.LogWarning("未识别的业务命令: {cmd}", command);
                    return false;
            }

            // 写入 + 读取确认（ACK），超时/轮询间隔可按需调节
            return await WriteWithAckAsync(targetReg, targetVal,
                                           timeout: TimeSpan.FromMilliseconds(1500),
                                           interval: TimeSpan.FromMilliseconds(120));
        }
        #endregion

        #region 底层直写（HTTP 高级操作/测试用）
        public async Task<bool> SendCommandAsync(string register, int value)
        {
            try
            {
                await Task.Yield();
                return SetDevice(register, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendCommandAsync 写入 {reg} 失败", register);
                return false;
            }
        }
        #endregion

        #region 读取寄存器
        public async Task<int?> ReadIntRegisterAsync(string register)
        {
            try
            {
                await Task.Yield();
                if (TryGetDevice(register, out int val)) return val;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadIntRegisterAsync 读取 {reg} 失败", register);
                return null;
            }
        }

        public async Task<bool?> ReadBitRegisterAsync(string register)
        {
            try
            {
                var v = await ReadIntRegisterAsync(register);
                if (!v.HasValue) return null;
                return v.Value != 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadBitRegisterAsync 读取 {reg} 失败", register);
                return null;
            }
        }
        #endregion

        #region 页面状态：车辆 & BMS（当前临时统一 D300）
        public async Task<VehicleStatus> GetVehicleStatusAsync(int vehicleId)
        {
            // 临时方案：全部字段读同一个寄存器 D300（你稍后提供地址表后再细分）
            double v = await ReadAsDouble("D300");
            return new VehicleStatus
            {
                Speed = v,
                Voltage = v,
                Current = v,
                Noise = v,
                RunTimeHours = v,
                BatteryPercent = v
            };
        }

        public async Task<BatteryStatus> GetBatteryStatusAsync(int vehicleId)
        {
            // BMS 也来自 PLC；同样临时统一 D300
            double v = await ReadAsDouble("D300");
            return new BatteryStatus
            {
                BatteryPercent = v,
                WorkTimeHours = v,
                Voltage = v,
                Current = v,
                Temperature = v
            };
        }
        #endregion

        #region 汇总/摄像头列表（占位，避免编译错误；按需替换为真实来源）
        public async Task<IEnumerable<VehicleOverview>> GetAllVehicleOverviewsAsync()
        {
            double v = await ReadAsDouble("D300");
            return new[]
            {
                new VehicleOverview { VehicleId = 1, Online = true, Speed = v, BatteryPercent = v }
            };
        }

        public async Task<IEnumerable<CameraInfo>> GetCameraInfosAsync()
        {
            await Task.Yield();
            // 若摄像头在线状态也来自 PLC，可在此处读取对应位/字并返回
            return new[]
            {
                new CameraInfo { CameraId = 1, Online = true },
                new CameraInfo { CameraId = 2, Online = true },
                new CameraInfo { CameraId = 3, Online = true },
                new CameraInfo { CameraId = 4, Online = true }
            };
        }
        #endregion

        #region 内部工具：写入 + 读取确认（ACK）
        private async Task<bool> WriteWithAckAsync(string register, int value, TimeSpan timeout, TimeSpan interval)
        {
            if (!SetDevice(register, value))
            {
                _logger.LogWarning("写入 {reg}={val} 失败（底层返回false）", register, value);
                return false;
            }

            var expected = value != 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                var bit = await ReadBitRegisterAsync(register);
                if (bit.HasValue && bit.Value == expected) return true;
                await Task.Delay(interval);
            }

            _logger.LogWarning("写入 {reg}={val} 后未在超时内读到期望值", register, value);
            return false;
        }

        private async Task<double> ReadAsDouble(string d)
        {
            var v = await ReadIntRegisterAsync(d);
            return v.HasValue ? v.Value : double.NaN;
        }
        #endregion

        #region 你原来的 PLC I/O 封装（请替换为真实 SDK 调用）
        /// <summary>
        /// 写寄存器/位的方法。请用你的PLC库改写（例如 _plc.SetDevice(device, value) == 0）。
        /// 返回 true 表示写成功。
        /// </summary>
        private bool SetDevice(string device, int value)
        {
            // ====== 替换为真实调用 ======
            // int rc = _plc.SetDevice(device, value);
            // return rc == 0;
            _logger.LogInformation("[DEMO] SetDevice {dev} = {val}", device, value);
            return true;
        }

        /// <summary>
        /// 读寄存器/位的方法。请用你的PLC库改写（例如 _plc.GetDevice(device, ref v)）。
        /// 返回 true 表示读成功，out value 为读到的数值。
        /// </summary>
        private bool TryGetDevice(string device, out int value)
        {
            // ====== 替换为真实调用 ======
            // int v = 0;
            // int rc = _plc.GetDevice(device, ref v);
            // value = v;
            // return rc == 0;

            // DEMO：先返回固定值，便于端到端打通；请尽快替换为真实读取。
            value = 123;
            _logger.LogInformation("[DEMO] GetDevice {dev} -> {val}", device, value);
            return true;
        }
        #endregion
    }
}
