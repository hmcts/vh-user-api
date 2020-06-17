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
            var cacheItem = await _distributedCache.GetAsync(factory.ToString());

            if (cacheItem != null && cacheItem.Length != 0)
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(cacheItem));
            }

            var result = await factory();
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
            
            await _distributedCache.SetAsync(factory.ToString(), bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return result;
        }
    }
}