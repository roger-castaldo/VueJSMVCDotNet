using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using VueJSMVCDotNet.Handlers;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Options;

namespace VueJSMVCDotNet
{
    /// <summary>
    /// This is the middleware defined to intercept requests coming in and handle them when necessary
    /// </summary>
    public class VueMiddleware : IDisposable
    {
        private sealed record CachedResponse(string Content, string ContentType, DateTime Timestamp);

        internal delegate string? delRegisterSlowMethodInstance(string url, InjectableMethod method, object? model, object?[] pars, IRequestData requestData, ILogger? log);

        private readonly VueMiddlewareOptions options;
        /// <summary>
        /// The Options that were supplied to construct the VueMiddleware
        /// </summary>
        public VueMiddlewareOptions Options => options;
        private readonly IEnumerable<IRequestHandler> handlers;
        private readonly string compressedCore;
        private readonly IMemoryCache? cache;
        private readonly Dictionary<string, SlowMethodInstance> methodInstances = [];
        private readonly ReaderWriterLockSlim locker = new();
        private bool disposedValue;
        private readonly Timer cleanupTimer;
        private readonly List<Type> invalidModelTypes = [];
        private readonly RequestDelegate next;


        /// <summary>
        /// default constructor as per dotnet standards
        /// </summary>
        /// <param name="next">next delegate call as per dotnet standards</param>
        /// <param name="options">the supplied options for creating the middle ware</param>
        /// <param name="cache">optionally supplied caching mechanism to use</param>
        public VueMiddleware(RequestDelegate next, VueMiddlewareOptions options, IMemoryCache? cache = null)
        {
            if ((options.VueFilesOptions!=null||options.MessageOptions!=null) && options.FileProvider==null)
                throw new ArgumentNullException(nameof(options), $"{nameof(options.FileProvider)} must be provided");
            var log = options.LogWriter;
            options.SetVueMiddleware(this);
            this.options=options;
            this.cache=cache;
            this.next=next ??= new RequestDelegate(NotFound);
            cleanupTimer=new(new TimerCallback(CleanupTimer_Elapsed), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            StreamReader sr = new(typeof(JSHandler).Assembly.GetManifestResourceStream("VueJSMVCDotNet.Handlers.Model.JSGenerators.core.js")!);
            var builder = new StringBuilder();
            builder.Append(@$"import * as vue from ""{options.VueImportPath}"";
const securityHeaders = {{{string.Join(',', (this.options.VueModelsOptions?.SecurityHeaders?? []).Select(t => $"'{t.Replace("'", "\\'")}':null"))}}};
{sr.ReadToEnd()}");

            compressedCore = JSMinifier.Minify(builder.ToString());
            sr.Close();

            var slowMethodDelegate = new delRegisterSlowMethodInstance(RegisterSlowMethodInstance);

            handlers =
            [
                .. options.VueFilesOptions?.BaseURL.Split(';')
                .Where(t => !string.IsNullOrEmpty(t.Trim()))
                .Select(url =>
                    new VueFilesHandler(
                        options.FileProvider!,
                        url,
                        options.VueImportPath,
                        options.VueLoaderImportPath,
                        options.CoreJSImport??options.CoreJSURL,
                        options.CompressAllJS,
                        (url) => (handlers?.OfType<JSHandler>().FirstOrDefault()?.HandlesJSPath(url)??false)
                    )
                )
,
                .. options.MessageOptions?.BaseURL.Split(';')
                    .Where(url => !string.IsNullOrEmpty(url.Trim()))
                    .Select(url =>
                    new MessagesHandler(
                            options.FileProvider!,
                            url,
                            options.CompressAllJS,
                            options.CoreJSImport??options.CoreJSURL,
                            options.VueImportPath
                        )
                    )
            ];
            if (options.VueModelsOptions!=null)
            {
                handlers = handlers.Concat(
                    [
                        new JSHandler(options.VueModelsOptions.BaseURL,options.VueImportPath,options.CoreJSImport??options.CoreJSURL,options.VueModelsOptions.SessionFactory,options.CompressAllJS,log),
                        new LoadHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new LoadAllHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new SaveHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new DeleteHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new UpdateHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new ModelListCallHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new InstanceMethodHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log),
                        new StaticMethodHandler(options.VueModelsOptions.SessionFactory,slowMethodDelegate,options.VueModelsOptions.BaseURL,log)
                    ]
                );
                AssemblyAdded();
            }
        }

        private string? RegisterSlowMethodInstance(string url, InjectableMethod method, object? model, object?[] pars, IRequestData requestData, ILogger? log)
        {
            var result = $"{url}/{Guid.NewGuid()}".ToLower();
            locker.EnterWriteLock();
            try
            {
                SlowMethodInstance smi = new(method, model, pars, requestData, log);
                methodInstances.Add(result, smi);
            }
            catch (Exception e)
            {
                log?.LogError(e, "Attempting to register a slow method caused an error. {Message}", e.Message);
                result=null;
            }
            locker.ExitWriteLock();
            return result;
        }
        private void CleanupTimer_Elapsed(object? state)
        {
            locker.EnterWriteLock();
            string[] keys = new string[methodInstances.Count];
            methodInstances.Keys.CopyTo(keys, 0);
            keys.ForEach(key =>
            {
                if (methodInstances[key].IsExpired)
                {
                    SlowMethodInstance smi = methodInstances[key];
                    try { smi.Dispose(); }
                    catch (Exception ex)
                    {
                        options.LogWriter?.LogError(ex, "SlowMethodInstance diposal error {Message}", ex.Message);
                    }
                    methodInstances.Remove(key);
                }
                else if (methodInstances[key].IsFinished)
                    methodInstances.Remove(key);
            });
            locker.ExitWriteLock();
        }

        /// <summary>
        /// the dotnet required method for produce a dependency injectable middle ware that all calls go through
        /// </summary>
        /// <param name="context">the current httpcontext</param>
        /// <returns>a task</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (string.Equals(context.Request.Method, "GET", StringComparison.InvariantCultureIgnoreCase)
                && context.Request.Path.Equals(new PathString(options.CoreJSURL)))
            {
                context.Response.ContentType="text/javascript";
                context.Response.StatusCode= 200;
                await context.Response.WriteAsync(compressedCore);
                return;
            }
            if (string.Equals(context.Request.Method, "PULL", StringComparison.InvariantCultureIgnoreCase)
                && options.VueModelsOptions!=null)
            {
                string url = Utility.CleanURL(Utility.BuildURL(context, options.VueModelsOptions?.BaseURL));
                locker.EnterReadLock();
                methodInstances.TryGetValue(url.ToLower(), out SlowMethodInstance? smi);
                locker.ExitReadLock();
                if (!(smi?.IsExpired??true))
                {
                    await smi.HandleRequest(context);
                    if (smi.IsFinished)
                    {
                        locker.EnterWriteLock();
                        methodInstances.Remove(url.ToLower());
                        locker.ExitWriteLock();
                    }
                    return;
                }
                else
                {
                    if (smi?.IsExpired??false)
                    {
                        locker.EnterWriteLock();
                        methodInstances.Remove(url.ToLower());
                        try { smi.Dispose(); }
                        catch (Exception e)
                        {
                            options.LogWriter?.LogError(e, "SlowMethodInstance disposal error {Message}", e.Message);
                        }
                        locker.ExitWriteLock();
                    }
                }
            }
            var handler = handlers.SelectFirstOrDefault(h => h.HandlesRequest(context), h => h.Result);
            if (handler!=null)
            {
                if (handler.RequestHandler is ICachingRequestHandler handlerCachingRequestHandler)
                {
                    if (cache?.TryGetValue<CachedResponse>(handler.CacheURL!, out var cachedResponse)??false)
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
                        var cachableResponse = await handlerCachingRequestHandler.ProduceResponseAsync(context, handler.State);
                        if (cachableResponse!=null)
                        {
                            cachedResponse = cache?.Set<CachedResponse>(handler.CacheURL!, new(cachableResponse.Content, cachableResponse.ContentType, cachableResponse.Timestamp), new MemoryCacheEntryOptions()
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
                            }, handler.CacheURL));
                            await OutputCachedResponse(context, cachableResponse.ContentType, cachableResponse.Timestamp, cachableResponse.Content);
                        }
                    }
                    return;
                }
                else if (handler.RequestHandler is INonCachingRequestHandler nonCachingRequestHandler)
                {
                    try
                    {
                        await nonCachingRequestHandler.ProduceResponseAsync(context, handler.State);
                    }
                    catch (CallNotFoundException cnfe)
                    {
                        options.LogWriter?.LogError(cnfe, "Request Error, call not found: {Message}", cnfe.Message);
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync(cnfe.Message);
                    }
                    catch (InsecureAccessException iae)
                    {
                        options.LogWriter?.LogError(iae, "Request Error, insecure access: {Message}", iae.Message);
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync(iae.Message);
                    }
                    catch (Exception e)
                    {
                        options.LogWriter?.LogError(e, "Request Error: {Message}", e.Message);
                        context.Response.ContentType= "text/text";
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Error");
                    }
                }
            }
            else
                await next(context);
        }
        private static async Task NotFound(HttpContext context)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Not Found");
        }

        private static async Task OutputCachedResponse(HttpContext context, string contentType, DateTime timestamp, string content)
        {
            context.Response.ContentType=contentType;
            context.Response.Headers.Append("Cache-Control", "public, must-revalidate, max-age=3600");
            context.Response.Headers.Append("Last-Modified", timestamp.ToUniversalTime().ToString("R"));
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(content);
        }


        internal void UnloadAssemblyContext(string? contextName)
        {
            var types = Utility.UnloadAssemblyContext(contextName)?? [];
            handlers.OfType<ITypeSensitiveHandler>().ForEach(h => h.UnloadTypes(types));
        }

        internal void AssemblyAdded()
        {
            handlers.OfType<ITypeSensitiveHandler>()
                .ForEach(h => h.ClearTypes());
            AssemblyLoadContext.All
                .ForEach(alc => AsssemblyLoadContextAdded(alc));
        }

        internal void AsssemblyLoadContextAdded(string? contextName)
        {
            var alc = AssemblyLoadContext.All.FirstOrDefault(alc => string.Equals(alc.Name, contextName, StringComparison.InvariantCultureIgnoreCase));
            if (alc!=null)
                AsssemblyLoadContextAdded(alc);
        }

        internal void AsssemblyLoadContextAdded(AssemblyLoadContext alc)
        {
            options.LogWriter?.LogDebug("Loading Assembly Load Context {Name}", alc.Name);
            IEnumerable<Exception> errors = DefinitionValidator.Validate(alc, options.LogWriter, out var invalidModels, out var models);
            invalidModelTypes.AddRange(invalidModels.Where(t => !invalidModelTypes.Contains(t)));
            if (errors.Any())
            {
                options.LogWriter?.LogError("Validation errors:");
                errors.ForEach(e => options.LogWriter?.LogError(e, "Validation Error: {Message}", e.Message));
                options.LogWriter?.LogError("Invalid IModels:");
                invalidModels.ForEach(t => options.LogWriter?.LogError("Invalid Model: {FullName}", t.FullName));
            }
            if (errors.Any() && !(options.VueModelsOptions?.IgnoreInvalidModels??false))
                throw new ModelValidationException(errors);
            models = models.Where(m => !invalidModelTypes.Contains(m));
            handlers.OfType<ITypeSensitiveHandler>()
                .ForEach(handler => handler.LoadTypes(models));
        }

        /// <summary>
        /// Called to properly clean up resources used by the middleware
        /// </summary>
        /// <param name="disposing">true if this disposing fully</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        cleanupTimer.Dispose();
                    }
                    catch (Exception e) { options.LogWriter?.LogError(e, "CleanupTimer disposal error {Message}", e.Message); }
                    locker.EnterWriteLock();
                    string[] keys = new string[methodInstances.Count];
                    methodInstances.Keys.CopyTo(keys, 0);
                    keys.ForEach(key =>
                    {
                        try { methodInstances[key].Dispose(); }
                        catch (Exception e) { options.LogWriter?.LogError(e, "Method Instance disposal error {Message}", e.Message); }
                        methodInstances.Remove(key);
                    });
                    locker.ExitWriteLock();
                    locker.Dispose();
                }
                disposedValue=true;
            }
        }

        /// <summary>
        /// The Disposal call to clean up used resources
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Static Middleware Extension to allow for Dependency Inject to occur, allowing for app.UseVueHandler
    /// in order to cause the library to be active in the request process
    /// </summary>
    [ExcludeFromCodeCoverage()]
    public static class VueMiddlewareExtension
    {
        /// <summary>
        /// call based on dotnet standards for middleware dependency injection
        /// </summary>
        /// <param name="builder">the application builder</param>
        /// <param name="options">the options used to define the middle ware settings</param>
        /// <returns>the application builder with the middleware setup</returns>
        public static IApplicationBuilder UseVueMiddleware(
            this IApplicationBuilder builder,
            VueMiddlewareOptions options) => builder.UseMiddleware<VueMiddleware>(options);
    }
}
