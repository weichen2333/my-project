using GuanHeBridgeMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace GuanHeBridgeMonitor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 我们希望EF Core管理的表
        public DbSet<AlarmEntry> AlarmEntries { get; set; }

        // 注意：其他模型(如VehicleStatus)是实时数据，
        // 我们不一定需要将它们存入数据库，除非是用于历史记录。
        // 目前我们只存储报警。
    }
}