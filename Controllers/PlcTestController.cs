using GuanHeBridgeMonitor.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text; // 用于构建日志

namespace GuanHeBridgeMonitor.Controllers
{
    /// <summary>
    /// 这是一个专用于调试的控制器，用于测试与 PLC（模拟器）的底层连接。
    /// 您可以通过访问 /api/PlcTest/full-link-test 来触发它。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PlcTestController : ControllerBase
    {
        private readonly IPlcService _plcService;
        private readonly ILogger<PlcTestController> _logger;

        public PlcTestController(IPlcService plcService, ILogger<PlcTestController> logger)
        {
            _plcService = plcService;
            _logger = logger;
        }

        /// <summary>
        /// 执行一个完整的 PLC 读/写链路测试。
        /// 它会尝试写入 D2000 和 M3000，然后立即读回以验证。
        /// </summary>
        [HttpGet("full-link-test")]
        public async Task<IActionResult> FullLinkTest()
        {
            // 我们使用 StringBuilder 来构建一个详细的测试报告
            var report = new StringBuilder();
            bool isSuccess = true;

            _logger.LogInformation("--- PLC 链路测试已启动 ---");
            report.AppendLine("--- PLC 链路测试报告 ---");

            try
            {
                // --- 1. 测试 D 寄存器 (整数) ---
                const string dRegister = "D200";
                const int testValue = 1;
                report.AppendLine($"[测试 D 区] 目标: {dRegister}");

                // 1a. 写入 D2000
                report.Append($"  -> 步骤 1: 写入值 {testValue}... ");
                bool writeD_Success = await _plcService.SendCommandAsync(dRegister, testValue);
                report.AppendLine(writeD_Success ? "成功 (OK)" : "失败 (FAIL)");
                if (!writeD_Success) isSuccess = false;

                // 1b. 读回 D2000
                report.Append($"  -> 步骤 2: 读回值... ");
                int? readD_Value = await _plcService.ReadIntRegisterAsync(dRegister);
                if (readD_Value.HasValue)
                {
                    report.AppendLine($"成功 (OK)。读回: {readD_Value.Value}");
                    if (readD_Value.Value != testValue)
                    {
                        isSuccess = false;
                        report.AppendLine($"  -> [验证失败] 读回的值 ({readD_Value.Value}) 与写入的值 ({testValue}) 不匹配!");
                    }
                }
                else
                {
                    isSuccess = false;
                    report.AppendLine("失败 (FAIL) (读取返回 null)");
                }

                // --- 2. 测试 M 寄存器 (位) ---
                const string mRegister = "D201";
                report.AppendLine($"\n[测试 M 区] 目标: {mRegister}");

                // 2a. 写入 M3000 (ON)
                report.Append($"  -> 步骤 3: 写入值 1 (ON)... ");
                bool writeM_On_Success = await _plcService.SendCommandAsync(mRegister, 1);
                report.AppendLine(writeM_On_Success ? "成功 (OK)" : "失败 (FAIL)");
                if (!writeM_On_Success) isSuccess = false;

                // 2b. 读回 M3000
                report.Append($"  -> 步骤 4: 读回值 (应为 ON)... ");
                bool? readM_On_Value = await _plcService.ReadBitRegisterAsync(mRegister);

                if (readM_On_Value.HasValue)
                {
                    report.AppendLine($"成功 (OK)。读回: {readM_On_Value.Value}");
                    if (readM_On_Value.Value != true)
                    {
                        isSuccess = false;
                        report.AppendLine($"  -> [验证失败] 写入 1 后，读回的不是 'true'!");
                    }
                }
                else
                {
                    isSuccess = false;
                    report.AppendLine("失败 (FAIL) (读取返回 null)");
                }

                // 2c. 写入 M3000 (OFF) (清理)
                report.Append($"  -> 步骤 5: 写入值 0 (OFF)... ");
                await _plcService.SendCommandAsync(mRegister, 0);
                report.AppendLine("完成 (清理)。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PLC 链路测试期间发生严重异常");
                report.AppendLine($"\n--- [严重错误] ---");
                report.AppendLine(ex.Message);
                report.AppendLine("测试被迫中止。这通常意味着 RealPlcService 构造函数中的 'Open()' 失败了。");
                isSuccess = false;
            }

            report.AppendLine("\n--- [测试结论] ---");
            report.AppendLine(isSuccess ? "✅ 所有读写链路均通畅。" : "❌ 测试失败。请检查日志和上述报告。");

            // 以纯文本形式返回报告，易于阅读
            return Content(report.ToString(), "text/plain", Encoding.UTF8);
        }
    }
}