using System.Text.Json;
using Jiten.Core.Data;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Jiten.Parser.Data.Redis;

public class RedisDeckWordCache : IDeckWordCache
{
    private readonly IDatabase _redisDb;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RedisDeckWordCache(IConfiguration configuration)
    {
        var connection = ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
        _redisDb = connection.GetDatabase();
    }

    private string BuildRedisKey(DeckWordCacheKey key)
    {
        return $"deckword:{key.Text}:{key.PartOfSpeech}:{key.DictionaryForm}";
    }

    public async Task<DeckWord?> GetAsync(DeckWordCacheKey key)
    {
        var redisKey = BuildRedisKey(key);
        var json = await _redisDb.StringGetAsync(redisKey);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<DeckWord>(json!, _jsonOptions);
    }

    public async Task SetAsync(DeckWordCacheKey key, DeckWord word, CommandFlags flags = CommandFlags.None)
    {
        var redisKey = BuildRedisKey(key);
        var json = JsonSerializer.Serialize(word, _jsonOptions);

        await _redisDb.StringSetAsync(redisKey, json, expiry: TimeSpan.FromDays(30), flags: flags);
    }
}