using System;
using System.Threading.Tasks;

namespace UserApi.Caching
{
    public interface ICache
    {
        Task<T> GetOrAddAsync<T>(Func<Task<T>> factory);
    }
}