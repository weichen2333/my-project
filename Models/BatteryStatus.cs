namespace GuanHeBridgeMonitor.Models
{
    // 对应 “蓄电池状态” 板块
    public class BatteryStatus
    {
        public int VehicleId { get; set; }
        public double BatteryPercent { get; set; }
        public double WorkTimeHours { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Temperature { get; set; }
    }
}