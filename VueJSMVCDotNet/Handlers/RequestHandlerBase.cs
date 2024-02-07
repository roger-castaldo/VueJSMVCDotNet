using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
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

        protected static async Task<bool> ReponseCached(HttpContext context, CachedContent cc)
        {
            if (cc!=null)
            {
                if (context.Request.Headers.ContainsKey("If-Modified-Since"))
                {
                    if (cc.Timestamp.ToUniversalTime().ToString("R").Equals(context.Request.Headers["If-Modified-Since"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        context.Response.ContentType="text/javascript";
                        context.Response.Headers.Append("accept-ranges", "bytes");
                        context.Response.Headers.Append("date", cc.Timestamp.ToUniversalTime().ToString("R"));
                        context.Response.Headers.Append("etag", $"\"{BitConverter.ToString(MD5.HashData(System.Text.ASCIIEncoding.ASCII.GetBytes(cc.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
                        context.Response.StatusCode = 304;
                        await context.Response.WriteAsync("");
                        return true;
                    }
                }
            }
            return false;
        }

        protected static async Task ProduceResponse(HttpContext context, string contentType, DateTime timestamp, string content)
        {
            context.Response.Headers.Append("Cache-Control", "public, must-revalidate, max-age=3600");
            context.Response.Headers.Append("Last-Modified", timestamp.ToUniversalTime().ToString("R"));
            context.Response.ContentType = contentType;
            await context.Response.WriteAsync(content);
        }

        protected static async Task ProduceNotFound(HttpContext context, string message)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync(message);
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
