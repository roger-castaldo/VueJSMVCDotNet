using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers
{
    internal abstract class RequestHandlerBase : IDisposable
    {
        public static readonly MemoryCacheEntryOptions CACHE_ENTRY_OPTIONS = new MemoryCacheEntryOptions()
        {
            SlidingExpiration=TimeSpan.FromHours(1)
        };

        protected readonly RequestDelegate _next;
        protected readonly ILog log;
        private readonly IMemoryCache _cache;
        private bool _isDisposed = false;

        public RequestHandlerBase(RequestDelegate next, IMemoryCache cache,ILog log)
        {
            _next = next;
            _cache = cache;
            this.log=log;
        }

        protected CachedContent? this[string url]
        {
            get
            {
                if (_isDisposed)
                    return null;
                return (CachedContent?)(_cache.TryGetValue(url, out var cachedContent) ? cachedContent : null);
            }
            set
            {
                if (!_isDisposed)
                {
                    if (value==null)
                        _cache.Remove(url);
                    else
                    {
                        try
                        {
                            _cache.Set(url, value, CACHE_ENTRY_OPTIONS);
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        public void Dispose()
        {
            _isDisposed=true;
            _dispose();
        }

        protected abstract void _dispose();
        public abstract Task ProcessRequest(HttpContext context);
    }
}
