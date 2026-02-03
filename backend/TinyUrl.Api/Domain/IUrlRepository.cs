using TinyUrl.Api.Models;

namespace TinyUrl.Api.Domain;

public interface IUrlRepository
{
    bool TryAdd(UrlMapping mapping);
    UrlMapping? GetByShortCode(string shortCode);

    // Cache: (ownerId,longUrl) -> shortCode
    string? GetCachedShortCode(string ownerId, string longUrl);
    void CacheShortCode(string ownerId, string longUrl, string shortCode);
    void RemoveCache(string ownerId, string longUrl);

    bool RemoveByShortCode(string shortCode);

    IEnumerable<UrlMapping> ListByOwner(string ownerId);

    bool ShortCodeExists(string shortCode);
}
