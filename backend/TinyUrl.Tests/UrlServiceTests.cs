using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyUrl.Api.Domain;
using TinyUrl.Api.Infrastructure.Services;
using TinyUrl.Api.Models;
using Xunit;

namespace TinyUrl.Tests;

public sealed class UrlServiceTests
{
    private class FakeRepo : IUrlRepository
    {
        public Func<string, string, string?>? GetCachedShortCodeImpl;
        public Func<string, UrlMapping?>? GetByShortCodeImpl;
        public Action<string, string, string?>? CacheShortCodeImpl;
        public Action<string, string>? RemoveCacheImpl;
        public Func<string, bool>? ShortCodeExistsImpl;
        public Func<UrlMapping, bool>? TryAddImpl;
        public Func<string, bool>? RemoveByShortCodeImpl;
        public Func<string, IEnumerable<UrlMapping>>? ListByOwnerImpl;

        public bool GetCachedShortCodeCalled;
        public bool GetByShortCodeCalled;
        public bool CacheShortCodeCalled;

        public bool ShortCodeExistsCalled;

        public bool TryAddCalled;

        public bool RemoveByShortCodeCalled;

        public bool ListByOwnerCalled;

        public bool ShortCodeExists(string shortCode)
        {
            ShortCodeExistsCalled = true;
            if (ShortCodeExistsImpl is null) throw new InvalidOperationException("ShortCodeExists not configured");
            return ShortCodeExistsImpl(shortCode);
        }

        public string? GetCachedShortCode(string ownerId, string longUrl)
        {
            GetCachedShortCodeCalled = true;
            if (GetCachedShortCodeImpl is null) return null;
            return GetCachedShortCodeImpl(ownerId, longUrl);
        }

        public void CacheShortCode(string ownerId, string longUrl, string shortCode)
        {
            CacheShortCodeCalled = true;
            CacheShortCodeImpl?.Invoke(ownerId, longUrl, shortCode);
        }

        public void RemoveCache(string ownerId, string longUrl) => RemoveCacheImpl?.Invoke(ownerId, longUrl);

        public bool TryAdd(UrlMapping mapping)
        {
            TryAddCalled = true;
            if (TryAddImpl is null) throw new InvalidOperationException("TryAdd not configured");
            return TryAddImpl(mapping);
        }

        public UrlMapping? GetByShortCode(string shortCode)
        {
            GetByShortCodeCalled = true;
            if (GetByShortCodeImpl is null) return null;
            return GetByShortCodeImpl(shortCode);
        }

        public bool RemoveByShortCode(string shortCode)
        {
            RemoveByShortCodeCalled = true;
            if (RemoveByShortCodeImpl is null) return false;
            return RemoveByShortCodeImpl(shortCode);
        }

        public IEnumerable<UrlMapping> ListByOwner(string ownerId)
        {
            ListByOwnerCalled = true;
            if (ListByOwnerImpl is null) return Enumerable.Empty<UrlMapping>();
            return ListByOwnerImpl(ownerId);
        }
    }

    private class FakeCodeGen : ICodeGenerator
    {
        private readonly Queue<string> _queue = new();
        public bool GenerateCalled;

        public FakeCodeGen(params string[] results)
        {
            foreach (var r in results) _queue.Enqueue(r);
        }

        public string Generate(int length)
        {
            GenerateCalled = true;
            if (_queue.Count > 0) return _queue.Dequeue();
            throw new InvalidOperationException("No more generated values configured");
        }
    }

    [Fact]
    public async Task CreateAsync_WhenSameOwnerAndLongUrl_ReturnsCached()
    {
        var repo = new FakeRepo();
        var codeGen = new FakeCodeGen();

        var owner = "u1";
        var longUrl = "https://example.com/a";

        repo.GetCachedShortCodeImpl = (o, l) => "abc12345";
        repo.GetByShortCodeImpl = sc => new UrlMapping
        {
            OwnerId = owner,
            LongUrl = longUrl,
            ShortCode = "abc12345"
        };

        var service = new UrlService(repo, codeGen);

        var result = await service.CreateAsync(owner, new CreateUrlRequest { LongUrl = longUrl }, "http://localhost:5000");

        Assert.Equal("abc12345", result.ShortCode);
        Assert.Equal(longUrl, result.LongUrl);
        Assert.True(repo.GetCachedShortCodeCalled);
        Assert.True(repo.GetByShortCodeCalled);
        Assert.False(codeGen.GenerateCalled);
    }

    [Fact]
    public async Task CreateAsync_WithCustomCode_EnforcesUniqueness()
    {
        var repo = new FakeRepo();
        var codeGen = new FakeCodeGen();

        var owner = "u1";
        var longUrl = "https://example.com/a";

        repo.GetCachedShortCodeImpl = (o, l) => null;
        repo.ShortCodeExistsImpl = sc => sc == "My_Code";

        var service = new UrlService(repo, codeGen);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(owner, new CreateUrlRequest { LongUrl = longUrl, CustomShortCode = "My_Code" }, "http://localhost"));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(repo.GetCachedShortCodeCalled);
        Assert.True(repo.ShortCodeExistsCalled);
        Assert.False(codeGen.GenerateCalled);
    }

    [Fact]
    public async Task CreateAsync_RandomGeneration_UsesGeneratorUntilUnique()
    {
        var repo = new FakeRepo();
        var codeGen = new FakeCodeGen("dupcode1", "uniqcode");

        var owner = "u1";
        var longUrl = "https://example.com/a";

        repo.GetCachedShortCodeImpl = (o, l) => null;
        repo.ShortCodeExistsImpl = sc => sc == "dupcode1";
        repo.TryAddImpl = m => m.ShortCode == "uniqcode" && m.LongUrl == longUrl && m.OwnerId == owner;
        repo.CacheShortCodeImpl = (o, l, s) => { };

        var service = new UrlService(repo, codeGen);

        var res = await service.CreateAsync(owner, new CreateUrlRequest { LongUrl = longUrl }, "http://localhost");

        Assert.Equal("uniqcode", res.ShortCode);
        Assert.True(repo.GetCachedShortCodeCalled);
        Assert.True(repo.TryAddCalled);
        Assert.True(codeGen.GenerateCalled);
    }

    [Fact]
    public async Task DeleteAsync_OnlyOwnerCanDelete()
    {
        var repo = new FakeRepo();
        var codeGen = new FakeCodeGen();

        repo.GetByShortCodeImpl = sc => new UrlMapping
        {
            OwnerId = "ownerA",
            LongUrl = "https://example.com",
            ShortCode = "abc"
        };

        var service = new UrlService(repo, codeGen);

        var ok = await service.DeleteAsync("ownerB", "abc");
        Assert.False(ok);

        Assert.True(repo.GetByShortCodeCalled);
        Assert.False(repo.RemoveByShortCodeCalled);
    }

    [Fact]
    public async Task ResolveAsync_IncrementsClicks()
    {
        var repo = new FakeRepo();
        var codeGen = new FakeCodeGen();

        var mapping = new UrlMapping { OwnerId = "o1", LongUrl = "https://example.com", ShortCode = "abc" };
        repo.GetByShortCodeImpl = sc => mapping;

        var service = new UrlService(repo, codeGen);

        var r1 = await service.ResolveAsync("anyOwnerId", "abc");
        var r2 = await service.ResolveAsync("anyOwnerId", "abc");

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.Equal(2, mapping.Clicks);

        Assert.True(repo.GetByShortCodeCalled);
    }
}
