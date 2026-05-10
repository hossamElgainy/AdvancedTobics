using AdvancedTobics.Models;
using AdvancedTobics.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnection))
{
    throw new InvalidOperationException(
        "Set ConnectionStrings:Redis in appsettings.json. For Memurai use: 127.0.0.1:6379");
}

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisConnection, ignoreUnknown: true);
    options.AbortOnConnectFail = false;
    options.ConnectRetry = 3;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    options.AsyncTimeout = 5000;
    options.ReconnectRetryPolicy = new ExponentialRetry(5000);

    return ConnectionMultiplexer.Connect(options);
});
builder.Services.AddScoped<RedisCacheService>();

builder.Services.Configure<RedisRateLimitOptions>(
    builder.Configuration.GetSection(RedisRateLimitOptions.SectionName));
builder.Services.AddSingleton<IRedisRateLimiter, RedisRateLimiter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<RedisRateLimitingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
