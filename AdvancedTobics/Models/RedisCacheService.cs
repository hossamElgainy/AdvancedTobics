using StackExchange.Redis;
using System.Text.Json;

namespace AdvancedTobics.Models
{
    public class RedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task SetDataAsync<T>(
            string key,
            T data,
            TimeSpan? expiry = null)
        {
            if (!_redis.IsConnected) return;

            try
            {
                var database = _redis.GetDatabase();
                var jsonData = JsonSerializer.Serialize(data);

                await database.StringSetAsync(
                    key,
                    jsonData,
                    expiry: expiry.HasValue ? (Expiration)expiry.Value : default);
            }
            catch (RedisConnectionException)
            {
                // Redis is down/unreachable; treat cache as best-effort
            }
            catch (RedisTimeoutException)
            {
                // Redis is slow/unreachable; treat cache as best-effort
            }
        }

        public async Task<T?> GetDataAsync<T>(string key)
        {
            if (!_redis.IsConnected) return default;

            try
            {
                var database = _redis.GetDatabase();
                var value = await database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                    return default;

                return JsonSerializer.Deserialize<T>(value!);
            }
            catch (RedisConnectionException)
            {
                return default;
            }
            catch (RedisTimeoutException)
            {
                return default;
            }
        }

        public async Task RemoveDataAsync(string key)
        {
            if (!_redis.IsConnected) return;

            try
            {
                var database = _redis.GetDatabase();
                await database.KeyDeleteAsync(key);
            }
            catch (RedisConnectionException)
            {
            }
            catch (RedisTimeoutException)
            {
            }
        }
    }
}
