using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Security.Cryptography;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints
{
    internal abstract class ACachingEndpoint(ILogger? logger, IMemoryCache? cache)
        : AEndpoint(logger)
    {
        private sealed record CachedResponse(string Content, string ContentType, DateTime Timestamp);
        internal static async Task OutputCachedResponse(HttpContext context, string contentType, DateTime timestamp, string content)
        {
            context.Response.ContentType=contentType;
            context.Response.Headers.Append("Cache-Control", "public, must-revalidate, max-age=3600");
            context.Response.Headers.Append("Last-Modified", timestamp.ToUniversalTime().ToString("R"));
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(content);
        }
        protected abstract Task<CachableResponse?> ProduceCachableResponseAsync(HttpContext context);

        public async Task ExecuteRequestAsync(HttpContext context)
        {
            var cacheURL = Convert.ToBase64String(SHA512.HashData(UTF8Encoding.UTF8.GetBytes(context.Request.Path.ToString())));
            if (cache?.TryGetValue<CachedResponse>(cacheURL, out var cachedResponse)??false)
            {
                if (context.Request.Headers.TryGetValue("If-Modified-Since", out var modifiedSince)
                            && (
                                string.Equals(modifiedSince.ToString().Trim(), cachedResponse!.Timestamp.ToUniversalTime().ToString("R"))
                                || cachedResponse.Timestamp.ToUniversalTime()>=DateTime.Parse(modifiedSince!, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal)
                            )
                        )
                {
                    context.Response.ContentType=cachedResponse.ContentType;
                    context.Response.Headers.Append("accept-ranges", "bytes");
                    context.Response.Headers.Append("date", cachedResponse.Timestamp.ToUniversalTime().ToString("R"));
                    context.Response.Headers.Append("etag", $"\"{BitConverter.ToString(MD5.HashData(System.Text.ASCIIEncoding.ASCII.GetBytes(cachedResponse.Timestamp.ToUniversalTime().ToString("R")))).Replace("-", "").ToLower()}\"");
                    context.Response.StatusCode = 304;
                    await context.Response.WriteAsync("");
                }
                else
                    await OutputCachedResponse(context, cachedResponse!.ContentType, cachedResponse.Timestamp, cachedResponse.Content);
            }
            else
            {
                var cachableResponse = await ProduceCachableResponseAsync(context);
                if (cachableResponse!=null)
                {
                    cachedResponse = cache?.Set<CachedResponse>(cacheURL, new(cachableResponse.Content, cachableResponse.ContentType, cachableResponse.Timestamp), new MemoryCacheEntryOptions()
                    {
                        SlidingExpiration=TimeSpan.FromHours(1),
                        AbsoluteExpiration=DateTimeOffset.UtcNow.AddHours(12)
                    });
                    (cachableResponse.ChangeTokens?? []).ForEach(token => token.RegisterChangeCallback((state) =>
                    {
                        try
                        {
                            cache?.Remove(state!);
                        }
                        catch (Exception)
                        {
                            //Ignoring the error because the cache may be disposed at this point and do not want it to cause a failure
                        }
                    }, cacheURL));
                    await OutputCachedResponse(context, cachableResponse.ContentType, cachableResponse.Timestamp, cachableResponse.Content);
                }
            }
        }
    }
}
