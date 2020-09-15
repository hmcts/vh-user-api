using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using UserApi.Caching;

namespace UserApi.UnitTests.Caching
{
    [TestFixture]
    public class GenericDistributedCacheTests
    {
        private readonly Mock<IDistributedCache> _cache;
        private readonly GenericDistributedCache _genericDistributedCache;
        
        public GenericDistributedCacheTests()
        {
            _cache = new Mock<IDistributedCache>(); 
            _genericDistributedCache = new GenericDistributedCache(_cache.Object);
        }

        [Test]
        public async Task GetOrAddAsync_adds_to_cache_when_not_in_cache()
        {
            const string objectToCache = "some object value";
            
            Func<Task<string>> factory = () => Task.FromResult(objectToCache);
            _cache.Setup(x => x.GetAsync(factory.ToString(), CancellationToken.None)).ReturnsAsync((byte[]) null);
            _cache.Setup
            (
                x => x.SetAsync
                (
                    factory.ToString(), 
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToCache)), 
                    It.IsAny<DistributedCacheEntryOptions>(), CancellationToken.None
                )
            );

            var result = await _genericDistributedCache.GetOrAddAsync(factory);

            result.Should().NotBeNull();
            result.Should().Be(objectToCache);
        }
        
        [Test]
        public async Task GetOrAddAsync_adds_to_cache_when_item_in_cache_is_empty_byte_array()
        {
            const string objectToCache = "some object value";
            
            Func<Task<string>> factory = () => Task.FromResult(objectToCache);
            _cache.Setup(x => x.GetAsync(factory.ToString(), CancellationToken.None)).ReturnsAsync(new byte[]{});
            _cache.Setup
            (
                x => x.SetAsync
                (
                    factory.ToString(), 
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToCache)), 
                    It.IsAny<DistributedCacheEntryOptions>(), CancellationToken.None
                )
            );

            var result = await _genericDistributedCache.GetOrAddAsync(factory);

            result.Should().NotBeNull();
            result.Should().Be(objectToCache);
        }
        
        [Test]
        public async Task GetOrAddAsync_return_item_in_cache()
        {
            const string objectToCache = "some object value";
            
            Func<Task<string>> factory = () => Task.FromResult(objectToCache);
            _cache.Setup(x => x.GetAsync(factory.ToString(), CancellationToken.None))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToCache)));

            var result = await _genericDistributedCache.GetOrAddAsync(factory);

            result.Should().NotBeNull();
            result.Should().Be(objectToCache);
        }

        [Test]
        public async Task GetOrAddAsync_return_default_value_when_factory_returns_null()
        {
            Func<Task<string>> factory = () => Task.FromResult((string)null);
            _cache.Setup(x => x.GetAsync(factory.ToString(), CancellationToken.None)).ReturnsAsync(new byte[] { });

            var result = await _genericDistributedCache.GetOrAddAsync(factory);

            result.Should().BeNull();
        }

        [Test]
        public async Task RefreshAsync_refreshes_cache()
        {
            const string objectToCache = "some object value";

            Func<Task<string>> factory = () => Task.FromResult(objectToCache);
            _cache.Setup(x => x.GetAsync(factory.ToString(), CancellationToken.None)).ReturnsAsync((byte[])null);
            _cache.Setup
            (
                x => x.SetAsync
                (
                    factory.ToString(),
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(objectToCache)),
                    It.IsAny<DistributedCacheEntryOptions>(), CancellationToken.None
                )
            );

            var method = _genericDistributedCache.RefreshCacheAsync(factory);
            await method;
            method.IsCompletedSuccessfully.Should().BeTrue();
        }
    }
}