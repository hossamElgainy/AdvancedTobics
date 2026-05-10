using StackExchange.Redis;
using System.Text.Json;

namespace AdvancedTobics.Models
{
    public class RedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task SetDataAsync<T>(
            string key,
            T data,
            TimeSpan? expiry = null)
        {
            var jsonData = JsonSerializer.Serialize(data);

            await _database.StringSetAsync(
                key,
                jsonData,
                (Expiration)expiry);
        }

        public async Task<T?> GetDataAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task RemoveDataAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
