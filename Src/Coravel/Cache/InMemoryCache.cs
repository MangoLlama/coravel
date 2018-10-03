using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Coravel.Cache.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Coravel.Cache
{
    public class InMemoryCache : ICache
    {
        private IMemoryCache _cache;
        private HashSet<string> _keys;

        public InMemoryCache(IMemoryCache cache)
        {
            this._cache = cache;
            this._keys = new HashSet<string>();
        }

        public TResult Get<TResult>(string key)
        {
            return this.Get(key, default(TResult));
        }

        public TResult Get<TResult>(string key, TResult @default)
        {
            if (!this._cache.TryGetValue(key, out var value)) return @default;

            if (value is TResult result) return result;

            return @default;
        }

        public async Task<TResult> GetAsync<TResult>(string key)
        {
            return await this.GetAsync(key, default(TResult));
        }

        public async Task<TResult> GetAsync<TResult>(string key, TResult @default)
        {
            return this.Get(key, @default);
        }

        public bool Has(string key)
        {
            return this._keys.Contains(key);
        }

        public TResult Remember<TResult>(string key, Func<TResult> cacheFunc, TimeSpan expiresIn)
        {
            this._keys.Add(key);

            return this._cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.Now.Add(expiresIn);
                return cacheFunc();
            });
        }

        public async Task<TResult> RememberAsync<TResult>(string key, Func<Task<TResult>> cacheFunc, TimeSpan expiresIn)
        {
            this._keys.Add(key);

            return await this._cache.GetOrCreateAsync(key, async entry =>
           {
               entry.AbsoluteExpiration = DateTimeOffset.Now.Add(expiresIn);
               return await cacheFunc();
           });
        }

        public TResult Forever<TResult>(string key, Func<TResult> cacheFunc)
        {
            this._keys.Add(key);

            return this._cache.GetOrCreate(key, entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                return cacheFunc();
            });
        }

        public async Task<TResult> ForeverAsync<TResult>(string key, Func<Task<TResult>> cacheFunc)
        {
            this._keys.Add(key);

            return await this._cache.GetOrCreateAsync(key, async entry =>
           {
               entry.Priority = CacheItemPriority.NeverRemove;
               return await cacheFunc();
           });
        }

        public void Forget(string key)
        {
            this._cache.Remove(key);
            this._keys.Remove(key);
        }

        public void Flush() {
            foreach(string key in this._keys){
                this._cache.Remove(key);
            }

            this._keys.Clear();
        }
    }
}
