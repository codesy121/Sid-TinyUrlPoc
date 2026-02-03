using TinyUrl.Api.Domain;

namespace TinyUrl.Api.Infrastructure.Services;

public sealed class HeaderUserContext : IUserContext
{
    public const string HeaderName = "X-Client-Id";

    public string GetOrThrow(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
            throw new InvalidOperationException($"Missing required header: {HeaderName}");

        var ownerId = values.ToString().Trim();
        if (string.IsNullOrWhiteSpace(ownerId) || ownerId.Length > 64)
            throw new InvalidOperationException($"Invalid {HeaderName}");

        return ownerId;
    }
}
