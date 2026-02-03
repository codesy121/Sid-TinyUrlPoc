using TinyUrl.Api.Models;

namespace TinyUrl.Api.Domain;

public interface IUrlService
{
    Task<CreateUrlResponse> CreateAsync(string ownerId, CreateUrlRequest request, string baseUrl);
    Task<bool> DeleteAsync(string ownerId, string shortCode);
    Task<ResolveUrlResponse?> ResolveAsync(string ownerId, string shortCode);
    Task<UrlStatsResponse?> GetStatsAsync(string ownerId, string shortCode);
    Task<IReadOnlyList<UrlListItemResponse>> ListAsync(string ownerId, string baseUrl);
}
