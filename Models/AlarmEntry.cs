using System.ComponentModel.DataAnnotations;

namespace GuanHeBridgeMonitor.Models
{
    public class AlarmEntry
    {
        [Key]
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }

        // 修正: 为非空字符串提供默认值，消除 CS8618 警告
        public string Message { get; set; } = default!;

        public string? Duration { get; set; }
        public bool IsAcknowledged { get; set; }
    }
}