namespace GuanHeBridgeMonitor.Models
{
    /// <summary>
    /// 车辆总览数据（用于 8 台检查车总览面板）
    /// </summary>
    public class VehicleOverview
    {
        public int VehicleId { get; set; }

        /// <summary>
        /// 车辆是否在线/联机
        /// </summary>
        public bool Online { get; set; }   // ★ RealPlcService 用到了 Online

        /// <summary>
        /// 当前速度（可空：读取失败时为 null 或 NaN）
        /// </summary>
        public double? Speed { get; set; } // ★ RealPlcService 用到了 Speed

        /// <summary>
        /// 电量百分比（可空：读取失败时为 null 或 NaN）
        /// </summary>
        public double? BatteryPercent { get; set; } // ★ RealPlcService 用到了 BatteryPercent
    }
}
