using GuanHeBridgeMonitor.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GuanHeBridgeMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleControlController(IPlcService plcService, ILogger<VehicleControlController> logger) : ControllerBase
    {
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                // 改为读取 D300 作为连通性测试
                var ok = await plcService.ReadIntRegisterAsync("D300");
                if (ok.HasValue)
                {
                    return Ok(new { connected = true, sampleD300 = ok.Value, message = "PLC连接正常（读取 D300 成功）" });
                }
                return StatusCode(500, new { connected = false, message = "PLC 未连接或读取失败" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ping PLC 失败");
                return StatusCode(500, new { connected = false, message = ex.Message });
            }
        }

        [HttpPost("command")]
        public async Task<IActionResult> SendCommand([FromBody] PlcWriteCommandDto command)
        {
            if (command == null || string.IsNullOrEmpty(command.Register))
            {
                return BadRequest(new { message = "无效的命令格式" });
            }
            logger.LogInformation("HTTP API: 收到命令写入 {Register} 值为 {Value}", command.Register, command.Value);
            try
            {
                bool success = await plcService.SendCommandAsync(command.Register, command.Value);
                return success
                    ? Ok(new { message = "命令发送成功" })
                    : StatusCode(500, new { message = "命令在PLC端执行失败，请检查设备" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HTTP API 在执行命令时发生异常: {Register}", command.Register);
                return StatusCode(500, new { message = $"服务器内部错误: {ex.Message}" });
            }
        }
    }

    public class PlcWriteCommandDto
    {
        public string Register { get; set; } = default!;
        public int Value { get; set; }
    }
}
