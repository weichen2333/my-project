using Microsoft.AspNetCore.Mvc;

namespace GuanHeBridgeMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // 演示用：内存保存（进程重启即丢失）
        private static readonly Dictionary<string, string> _users = new(); // username -> password

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "用户名/密码不能为空" });

            lock (_users)
            {
                if (_users.ContainsKey(dto.Username))
                    return Conflict(new { message = "用户名已存在" });
                _users[dto.Username] = dto.Password;
            }
            return Ok(new { message = "注册成功" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            lock (_users)
            {
                if (_users.TryGetValue(dto.Username, out var pwd) && pwd == dto.Password)
                {
                    // 演示：颁发一个假 token，前端仅做存在性校验
                    return Ok(new { token = $"token_{Guid.NewGuid():N}", username = dto.Username });
                }
            }
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        public class RegisterDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
        public class LoginDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
    }
}
