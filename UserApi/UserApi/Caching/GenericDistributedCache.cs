using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace UserApi.Caching
{
    public class GenericDistributedCache : ICache
    {
        private readonly IDistributedCache _distributedCache;

        public GenericDistributedCache(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T> GetOrAddAsync<T>(Func<Task<T>> factory)
        {
            var key = factory.ToString();
            return await GetOrAddAsync(key, factory);
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        {
            var cacheItem = await _distributedCache.GetAsync(key);

            if (cacheItem != null && cacheItem.Length != 0)
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(cacheItem));
            }

            var result = await factory();

            if (result == null)
            {
                return default;
            }
            
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));

            await _distributedCache.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3)
            });

            return result;
        }

        public async Task RefreshCacheAsync<T>(Func<Task<T>> factory)
        {
            var key = factory.ToString();

            var result = await factory();

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));

            await _distributedCache.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3)
            });
        }
    }
}