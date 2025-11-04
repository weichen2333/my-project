namespace GuanHeBridgeMonitor.Models
{
    /// <summary>
    /// 摄像头状态信息（用于“40路摄像头状态”等）
    /// </summary>
    public class CameraInfo
    {
        public int CameraId { get; set; }

        /// <summary>
        /// 摄像头是否在线
        /// </summary>
        public bool Online { get; set; } // ★ RealPlcService 用到了 Online

        // 可选：名称/其它状态字段（按需扩展）
        public string? Name { get; set; }
        public string? Status { get; set; }
    }
}
