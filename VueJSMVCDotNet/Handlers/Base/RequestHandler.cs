using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Base
{
    internal abstract class RequestHandler() : IRequestHandler
    {
        protected bool disposedValue;

        protected static async Task<bool> ReponseCached(HttpContext context, CachedContent cc)
        {
            if (context.Request.Headers.TryGetValue("If-Modified-Since", out var mod))
            {
                System.Diagnostics.Trace.WriteLine(mod);
                System.Diagnostics.Trace.WriteLine(cc?.Timestamp.ToString("R"));
            }
            if (context.Request.Headers.TryGetValue("If-Modified-Since", out var modifiedSince) &&
                (
                    DateTime.Parse(modifiedSince.ToString()).Ticks <= (cc?.Timestamp.Ticks ?? long.MinValue)
                    || string.Equals(modifiedSince.ToString().Trim(), cc?.Timestamp.ToString("R"), StringComparison.InvariantCultureIgnoreCase)
                ))
            {
                context.Response.ContentType = "text/javascript";
                context.Response.Headers.Append("accept-ranges", "bytes");
                context.Response.Headers.Append("date", cc?.Timestamp.ToUniversalTime().ToString("R"));
                context.Response.Headers.Append("etag", $"\"{BitConverter.ToString(MD5.HashData(Encoding.ASCII.GetBytes(cc.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
                context.Response.StatusCode = 304;
                await context.Response.WriteAsync("");
                return true;
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

        protected abstract bool InternalHandlesRequest(HttpContext context, out object state, out string cacheURL);

        protected virtual void InternalDispose() { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    InternalDispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
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

        public IHandlesRequestResponse HandlesRequest(HttpContext context)
            => new HandlesRequestResponse(
                InternalHandlesRequest(context, out var state, out var cacheURL),
                state,
                cacheURL,
                this
            );
    }
}
