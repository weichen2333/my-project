namespace GuanHeBridgeMonitor.Models
{
    // 对应 “检查车运行状态” 板块
    public class VehicleStatus
    {
        public int VehicleId { get; set; }
        public double Speed { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Noise { get; set; }
        public double RunTimeHours { get; set; }
        public double BatteryPercent { get; set; }
    }
}