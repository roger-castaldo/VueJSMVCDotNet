using Microsoft.AspNetCore.Http;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Base
{
    internal abstract class RequestHandler() : IRequestHandler
    {
        protected bool disposedValue;

        protected static async Task ProduceNotFound(HttpContext context, string message)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync(message);
        }

        protected abstract bool InternalHandlesRequest(HttpContext context, out object? state, out string? cacheURL);

        protected virtual void InternalDispose() { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    InternalDispose();
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
