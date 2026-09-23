using GamePortal.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GamePortal.UnitTests.Infrastructure;

public class DistributedCacheServiceTests
{
    private readonly MemoryDistributedCache _memory = new(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task 캐시_적중시_팩토리를_호출하지_않는다()
    {
        var sut = new DistributedCacheService(_memory, NullLogger<DistributedCacheService>.Instance);
        var calls = 0;

        for (var i = 0; i < 3; i++)
        {
            await sut.GetOrCreateAsync("k", _ => Task.FromResult(++calls), TimeSpan.FromMinutes(1), default);
        }

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task null_결과도_캐싱한다()
    {
        var sut = new DistributedCacheService(_memory, NullLogger<DistributedCacheService>.Instance);
        var calls = 0;

        for (var i = 0; i < 2; i++)
        {
            await sut.GetOrCreateAsync<string?>("missing", _ => { calls++; return Task.FromResult<string?>(null); }, TimeSpan.FromMinutes(1), default);
        }

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task 리전_무효화시_버전이_바뀐다()
    {
        var sut = new DistributedCacheService(_memory, NullLogger<DistributedCacheService>.Instance);

        var v1 = await sut.GetRegionVersionAsync("notices", default);
        var v1Again = await sut.GetRegionVersionAsync("notices", default);
        await sut.InvalidateRegionAsync("notices", default);
        var v2 = await sut.GetRegionVersionAsync("notices", default);

        Assert.Equal(v1, v1Again);
        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public async Task 캐시_장애시_원본으로_폴백한다()
    {
        var broken = Substitute.For<IDistributedCache>();
        broken.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException("redis timeout"));
        broken.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("redis timeout"));
        var sut = new DistributedCacheService(broken, NullLogger<DistributedCacheService>.Instance);

        var value = await sut.GetOrCreateAsync("k", _ => Task.FromResult(42), TimeSpan.FromMinutes(1), default);
        var version = await sut.GetRegionVersionAsync("notices", default);

        Assert.Equal(42, value);
        Assert.Equal("fallback", version);
    }
}
