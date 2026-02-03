namespace TinyUrl.Api.Models;

public class CreateUrlRequest
{
    public string LongUrl { get; init; }
    public string? CustomShortCode { get; init; }
}

public class CreateUrlResponse
{
    public string ShortCode { get; init; }
    public string ShortUrl { get; init; }
    public string LongUrl { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public class ResolveUrlResponse
{
    public string ShortCode { get; init; }
    public string LongUrl { get; init; }
}

public class UrlStatsResponse
{
    public string ShortCode { get; init; }
    public string LongUrl { get; init; }
    public long Clicks { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? LastAccessedAtUtc { get; init; }
}

public class UrlListItemResponse
{
    public string ShortCode { get; init; }
    public string ShortUrl { get; init; }
    public string LongUrl { get; init; }
    public long Clicks { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
