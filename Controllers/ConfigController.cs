using GuanHeBridgeMonitor.Services.Config;
using Microsoft.AspNetCore.Mvc;

namespace GuanHeBridgeMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        [HttpGet("system")]
        public IActionResult GetSystem() =>
            Ok(new { vehicleMode = RuntimeConfig.VehicleMode, updateIntervalMs = RuntimeConfig.UpdateIntervalMs });

        public class SystemDto { public string VehicleMode { get; set; } = "Real"; public int UpdateIntervalMs { get; set; } = 2000; }

        [HttpPost("system")]
        public IActionResult SaveSystem([FromBody] SystemDto dto)
        {
            if (dto.UpdateIntervalMs < 500) dto.UpdateIntervalMs = 500;
            RuntimeConfig.VehicleMode = dto.VehicleMode;
            RuntimeConfig.UpdateIntervalMs = dto.UpdateIntervalMs;
            return Ok(new { message = "系统参数已保存" });
        }

        [HttpGet("cameras")]
        public IActionResult GetCameras() => Ok(RuntimeConfig.Cameras);

        public class SaveCamerasDto { public List<CameraCfg> Cameras { get; set; } = new(); }

        [HttpPost("cameras")]
        public IActionResult SaveCameras([FromBody] SaveCamerasDto dto)
        {
            if (dto?.Cameras == null || dto.Cameras.Count == 0) return BadRequest(new { message = "参数无效" });

            // 规范化 URL（防止用户填入反斜杠或相对路径）
            foreach (var c in dto.Cameras)
            {
                if (string.IsNullOrWhiteSpace(c.Url)) continue;
                c.Url = c.Url.Replace("\\", "/").Trim();
                if (!c.Url.StartsWith("/")) c.Url = "/" + c.Url;
                if (c.SourceType.Equals("hls", StringComparison.OrdinalIgnoreCase) && !c.Url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    // 尽力补齐（如果不是 m3u8 则保持用户输入）
                    if (!c.Url.Contains('.')) c.Url += ".m3u8";
                }
            }

            RuntimeConfig.Cameras.Clear();
            RuntimeConfig.Cameras.AddRange(dto.Cameras.OrderBy(x => x.Id));
            return Ok(new { message = "摄像头配置已保存" });
        }
    }
}
