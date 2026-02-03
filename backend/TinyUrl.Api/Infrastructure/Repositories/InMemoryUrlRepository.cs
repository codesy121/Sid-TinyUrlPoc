using System.Collections.Concurrent;
using TinyUrl.Api.Domain;
using TinyUrl.Api.Models;

namespace TinyUrl.Api.Infrastructure.Repositories;

public sealed class InMemoryUrlRepository : IUrlRepository
{
    private readonly ConcurrentDictionary<string, UrlMapping> _byCode = new(StringComparer.Ordinal);

    // Cache key: "{ownerId}|{longUrl}" -> shortCode
    private readonly ConcurrentDictionary<string, string> _cacheByOwnerLong = new(StringComparer.Ordinal);

    private static string CacheKey(string ownerId, string longUrl) => $"{ownerId}|{longUrl}";

    public bool TryAdd(UrlMapping mapping)
    {
        return _byCode.TryAdd(mapping.ShortCode, mapping);
    }

    public UrlMapping? GetByShortCode(string shortCode)
    {
        return _byCode.TryGetValue(shortCode, out var mapping) ? mapping : null;
    }

    public string? GetCachedShortCode(string ownerId, string longUrl)
    {
        return _cacheByOwnerLong.TryGetValue(CacheKey(ownerId, longUrl), out var code) ? code : null;
    }

    public void CacheShortCode(string ownerId, string longUrl, string shortCode)
    {
        _cacheByOwnerLong[CacheKey(ownerId, longUrl)] = shortCode;
    }

    public void RemoveCache(string ownerId, string longUrl)
    {
        _cacheByOwnerLong.TryRemove(CacheKey(ownerId, longUrl), out _);
    }

    public bool RemoveByShortCode(string shortCode)
    {
        if (_byCode.TryRemove(shortCode, out var removed))
        {
            // Best-effort cache cleanup for this owner+long
            RemoveCache(removed.OwnerId, removed.LongUrl);
            return true;
        }
        return false;
    }

    public IEnumerable<UrlMapping> ListByOwner(string ownerId)
    {
        return _byCode.Values.Where(v => string.Equals(v.OwnerId, ownerId, StringComparison.Ordinal));
    }

    public bool ShortCodeExists(string shortCode) => _byCode.ContainsKey(shortCode);
}
