namespace GuanHeBridgeMonitor.Services.Config
{
    public static class RuntimeConfig
    {
        // 系统参数
        public static string VehicleMode { get; set; } = "Real";
        public static int UpdateIntervalMs { get; set; } = 2000;

        // 4 路摄像头默认配置（可在“系统设置”里修改并保存）
        public static List<CameraCfg> Cameras { get; } = new()
        {
            new(){ Id=1, Name="前视", SourceType="hls",  Url="/streams/veh1_cam1.m3u8" },
            new(){ Id=2, Name="后视", SourceType="hls",  Url="/streams/veh1_cam2.m3u8" },
            new(){ Id=3, Name="左侧", SourceType="file", Url="/videos/sample_front.mp4" },
            new(){ Id=4, Name="右侧", SourceType="usb",  Url="" }
        };
    }

    public class CameraCfg
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        // hls | file | usb | webrtc
        public string SourceType { get; set; } = "hls";
        public string Url { get; set; } = "";
    }
}
