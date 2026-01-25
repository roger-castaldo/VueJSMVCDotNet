using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Javascript;

namespace VueJSMVCDotNet.Endpoints
{
    internal abstract class AJSEngineEndpoint(JSEngine? engine, ILogger? logger, IMemoryCache? cache) : ACachingEndpoint(logger, cache), IDisposable
    {
        private bool disposedValue;

        protected JSEngine GetEngine(HttpContext context)
            => engine??context.RequestServices.GetRequiredService<JSEngine>();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    engine?.Dispose();
                }
                disposedValue=true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
