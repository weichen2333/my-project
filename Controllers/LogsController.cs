using GuanHeBridgeMonitor.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuanHeBridgeMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public LogsController(ApplicationDbContext db) { _db = db; }

        // GET /api/logs?limit=200
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int limit = 200)
        {
            if (limit <= 0 || limit > 1000) limit = 200;
            var list = await _db.AlarmEntries
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
            return Ok(list);
        }

        // PUT /api/logs/ack/{id}
        [HttpPut("ack/{id:long}")]
        public async Task<IActionResult> Ack(long id)
        {
            var alarm = await _db.AlarmEntries.FirstOrDefaultAsync(a => a.Id == id);
            if (alarm == null) return NotFound();
            alarm.IsAcknowledged = true;
            await _db.SaveChangesAsync();
            return Ok(new { message = "已确认" });
        }
    }
}
