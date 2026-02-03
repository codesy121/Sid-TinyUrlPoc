namespace TinyUrl.Api.Domain;

public interface IUserContext
{
    // Anonymous user id from request. Throw if missing/invalid.
    string GetOrThrow(HttpContext httpContext);
}
