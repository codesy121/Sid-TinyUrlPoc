using System.Text.RegularExpressions;
using System.Threading;
using TinyUrl.Api.Domain;
using TinyUrl.Api.Models;

namespace TinyUrl.Api.Infrastructure.Services;

public sealed class UrlService : IUrlService
{
    private const int DefaultCodeLength = 8;
    private static readonly Regex CustomCodeRegex = new("^[A-Za-z0-9_-]{4,32}$", RegexOptions.Compiled);

    private readonly IUrlRepository _repo;
    private readonly ICodeGenerator _codeGen;

    public UrlService(IUrlRepository repo, ICodeGenerator codeGen)
    {
        _repo = repo;
        _codeGen = codeGen;
    }

    public Task<CreateUrlResponse> CreateAsync(string ownerId, CreateUrlRequest request, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("ownerId required", nameof(ownerId));
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.LongUrl)) throw new ArgumentException("LongUrl required", nameof(request));

        // Basic URL validation.
        if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var longUri) ||
            (longUri.Scheme != Uri.UriSchemeHttp && longUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("LongUrl must be an absolute http(s) URL");
        }

        // Cache requirement: same owner + same long URL -> same short code.
        var cachedCode = _repo.GetCachedShortCode(ownerId, request.LongUrl);
        if (!string.IsNullOrEmpty(cachedCode))
        {
            var existing = _repo.GetByShortCode(cachedCode);
            if (existing is not null && existing.OwnerId == ownerId && existing.LongUrl == request.LongUrl)
            {
                return Task.FromResult(ToCreateResponse(existing, baseUrl));
            }
            // Cache stale: fall through and regenerate.
            _repo.RemoveCache(ownerId, request.LongUrl);
        }

        // Custom code path.
        if (!string.IsNullOrWhiteSpace(request.CustomShortCode))
        {
            var custom = request.CustomShortCode.Trim();
            ValidateCustomCode(custom);

            if (_repo.ShortCodeExists(custom))
                throw new InvalidOperationException("Short code already exists");

            var mapping = new UrlMapping
            {
                OwnerId = ownerId,
                LongUrl = request.LongUrl,
                ShortCode = custom
            };

            if (!_repo.TryAdd(mapping))
                throw new InvalidOperationException("Failed to create short URL (collision)");

            _repo.CacheShortCode(ownerId, request.LongUrl, mapping.ShortCode);
            return Task.FromResult(ToCreateResponse(mapping, baseUrl));
        }

        // Randomly generate unique code.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = _codeGen.Generate(DefaultCodeLength);
            if (_repo.ShortCodeExists(code)) continue;

            var mapping = new UrlMapping
            {
                OwnerId = ownerId,
                LongUrl = request.LongUrl,
                ShortCode = code
            };

            if (_repo.TryAdd(mapping))
            {
                _repo.CacheShortCode(ownerId, request.LongUrl, mapping.ShortCode);
                return Task.FromResult(ToCreateResponse(mapping, baseUrl));
            }
        }

        throw new InvalidOperationException("Failed to generate a unique short code");
    }

    public Task<bool> DeleteAsync(string ownerId, string shortCode)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("ownerId required", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(shortCode)) throw new ArgumentException("shortCode required", nameof(shortCode));

        var existing = _repo.GetByShortCode(shortCode);
        if (existing is null) return Task.FromResult(false);

        // Only the creator (same anonymous ownerId) can delete.
        if (!string.Equals(existing.OwnerId, ownerId, StringComparison.Ordinal))
            return Task.FromResult(false);

        return Task.FromResult(_repo.RemoveByShortCode(shortCode));
    }

    public Task<ResolveUrlResponse?> ResolveAsync(string ownerId, string shortCode)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("ownerId required", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(shortCode)) throw new ArgumentException("shortCode required", nameof(shortCode));

        var mapping = _repo.GetByShortCode(shortCode);
        if (mapping is null) return Task.FromResult<ResolveUrlResponse?>(null);

        // Anyone can resolve (service is public). We still require ownerId for cache behavior elsewhere.
        Interlocked.Increment(ref mapping.Clicks);
        mapping.LastAccessedAtUtc = DateTimeOffset.UtcNow;

        return Task.FromResult<ResolveUrlResponse?>(new ResolveUrlResponse
        {
            ShortCode = mapping.ShortCode,
            LongUrl = mapping.LongUrl
        });
    }

    public Task<UrlStatsResponse?> GetStatsAsync(string ownerId, string shortCode)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("ownerId required", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(shortCode)) throw new ArgumentException("shortCode required", nameof(shortCode));

        var mapping = _repo.GetByShortCode(shortCode);
        if (mapping is null) return Task.FromResult<UrlStatsResponse?>(null);

        return Task.FromResult<UrlStatsResponse?>(new UrlStatsResponse
        {
            ShortCode = mapping.ShortCode,
            LongUrl = mapping.LongUrl,
            Clicks = Interlocked.Read(ref mapping.Clicks),
            CreatedAtUtc = mapping.CreatedAtUtc,
            LastAccessedAtUtc = mapping.LastAccessedAtUtc
        });
    }

    public Task<IReadOnlyList<UrlListItemResponse>> ListAsync(string ownerId, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("ownerId required", nameof(ownerId));

        var list = _repo.ListByOwner(ownerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new UrlListItemResponse
            {
                ShortCode = x.ShortCode,
                ShortUrl = $"{baseUrl.TrimEnd('/')}/r/{x.ShortCode}",
                LongUrl = x.LongUrl,
                Clicks = Interlocked.Read(ref x.Clicks),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<UrlListItemResponse>>(list);
    }

    private static void ValidateCustomCode(string code)
    {
        if (!CustomCodeRegex.IsMatch(code))
            throw new InvalidOperationException("CustomShortCode must be 4-32 chars: A-Z a-z 0-9 _ -");
    }

    private static CreateUrlResponse ToCreateResponse(UrlMapping mapping, string baseUrl) => new()
    {
        ShortCode = mapping.ShortCode,
        ShortUrl = $"{baseUrl.TrimEnd('/')}/r/{mapping.ShortCode}",
        LongUrl = mapping.LongUrl,
        CreatedAtUtc = mapping.CreatedAtUtc
    };
}
