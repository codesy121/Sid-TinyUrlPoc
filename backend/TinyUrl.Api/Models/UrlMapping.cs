namespace TinyUrl.Api.Models;

public sealed class UrlMapping
{
    public string ShortCode { get; init; }
    public string LongUrl { get; init; }

    // Anonymous "user" identity provided by the client (e.g., localStorage UUID).
    public  string OwnerId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    // Use Interlocked for thread-safe increments.
    public long Clicks;

    public DateTimeOffset? LastAccessedAtUtc { get; set; }
}
