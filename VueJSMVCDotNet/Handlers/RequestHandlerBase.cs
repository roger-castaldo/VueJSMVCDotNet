using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using VueJSMVCDotNet.Caching;

namespace VueJSMVCDotNet.Handlers
{
    internal abstract class RequestHandlerBase : IDisposable
    {
        public static readonly MemoryCacheEntryOptions CACHE_ENTRY_OPTIONS = new()
        {
            SlidingExpiration=TimeSpan.FromHours(1)
        };

        protected readonly RequestDelegate next;
        protected readonly ILogger log;
        private readonly IMemoryCache cache;
        protected bool disposedValue;

        public RequestHandlerBase(RequestDelegate next, IMemoryCache cache, ILogger log)
        {
            this.next = next;
            this.cache = cache;
            this.log=log;
        }

        protected CachedContent? this[string url]
        {
            get
            {
                if (disposedValue)
                    return null;
                return (CachedContent?)(cache.TryGetValue(url, out var cachedContent) ? cachedContent : null);
            }
            set
            {
                if (!disposedValue)
                {
                    if (value==null)
                        cache.Remove(url);
                    else
                    {
                        try
                        {
                            cache.Set(url, value, CACHE_ENTRY_OPTIONS);
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        public abstract Task ProcessRequest(HttpContext context);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue=true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RequestHandlerBase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
