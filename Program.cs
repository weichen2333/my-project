using GuanHeBridgeMonitor.Data;
using GuanHeBridgeMonitor.Hubs;
using GuanHeBridgeMonitor.Services.Interfaces;
using GuanHeBridgeMonitor.Services.Hardware; // 真实
using GuanHeBridgeMonitor.Workers;          // 轮询工人
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// 1. 连接字符串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. MVC/SignalR/CORS
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true)
              .AllowCredentials();
    });
});

// 4. 绑定真实服务（不再使用 Mock）
builder.Services.AddSingleton<RealPlcService>();
builder.Services.AddSingleton<IPlcService>(sp => sp.GetRequiredService<RealPlcService>());
builder.Services.AddSingleton<IBmsService>(sp => sp.GetRequiredService<RealPlcService>());

// 5. 后台轮询（替代 DataSimulatorWorker）
builder.Services.AddHostedService<PlcPollingWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();

// 为 HLS 添加 MIME & 缓存策略
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
provider.Mappings[".ts"] = "video/mp2t";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.PhysicalPath?.ToLowerInvariant();
        if (path is not null)
        {
            if (path.EndsWith(".m3u8"))
            {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate, proxy-revalidate";
                ctx.Context.Response.Headers[HeaderNames.Pragma] = "no-cache";
                ctx.Context.Response.Headers[HeaderNames.Expires] = "0";
            }
            else if (path.EndsWith(".ts"))
            {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=30";
            }
        }
    }
});

// 如果你采用外置 HLS 目录（可选）
var hlsRoot = builder.Configuration["Hls:Root"]; // 例如 C:\rtsp2hls\out
if (!string.IsNullOrWhiteSpace(hlsRoot) && Directory.Exists(hlsRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(hlsRoot),
        RequestPath = "/streams",
        ContentTypeProvider = provider,
        OnPrepareResponse = ctx =>
        {
            var path = ctx.File.PhysicalPath?.ToLowerInvariant();
            if (path is not null)
            {
                if (path.EndsWith(".m3u8"))
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate, proxy-revalidate";
                    ctx.Context.Response.Headers[HeaderNames.Pragma] = "no-cache";
                    ctx.Context.Response.Headers[HeaderNames.Expires] = "0";
                }
                else if (path.EndsWith(".ts"))
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=30";
                }
            }
        }
    });
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<BridgeMonitorHub>("/bridgeMonitorHub");

app.Run();
