using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers
{
    internal class ModelRequestHandler : RequestHandlerBase
    {
        private static readonly Regex baseUrlRegex = new("^(https?:/)?/(.+)(/)$", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        internal static DateTime StartTime { get; private set; } = DateTime.Now;

        internal enum RequestMethods
        {
            GET,
            PUT,
            DELETE,
            PATCH,
            METHOD,
            SMETHOD,
            PULL,
            LIST
        }

        public delegate string delRegisterSlowMethodInstance(string url, InjectableMethod method, object model, object[] pars, IRequestData requestData, ILogger log);

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private List<Type> invalidModels;
        private bool isInitialized=false;
        private readonly Dictionary<string, SlowMethodInstance> methodInstances;
        private readonly Timer cleanupTimer;
        private readonly ReaderWriterLockSlim locker;
        private readonly ModelRequestHandlerBase[] handlers;
        private readonly JSHandler jsHandler;
        private readonly string urlBase;
        private readonly bool ignoreInvalidModels;

        public ModelRequestHandler(ILogger log,
            string baseURL,
            bool ignoreInvalidModels,
            string vueImportPath,
            string coreImportPath,
            ISecureSessionFactory sessionFactory,
            bool compressAllJS,
            RequestDelegate next, IMemoryCache cache)
            : base(next, cache,log)
        {
            locker = new ReaderWriterLockSlim();
            urlBase=baseURL;
            this.ignoreInvalidModels=ignoreInvalidModels;
            vueImportPath??="https://unpkg.com/vue@3/dist/vue.esm-browser.js";
            if (urlBase!=null && !baseUrlRegex.IsMatch(urlBase))
            {
                if (!urlBase.EndsWith("/"))
                    urlBase+="/";
                if (urlBase!="/" && !baseUrlRegex.IsMatch(urlBase))
                    urlBase="/"+urlBase;
            }
            var registerSlowMethod = new delRegisterSlowMethodInstance(RegisterSlowMethodInstance);
            var instanceMethodHandler = new InstanceMethodHandler(next, sessionFactory, registerSlowMethod, urlBase,log);
            var deleteHandler = new DeleteHandler(new RequestDelegate(instanceMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod,urlBase, log);
            var updateHandler = new UpdateHandler(new RequestDelegate(deleteHandler.ProcessRequest),sessionFactory,registerSlowMethod, urlBase, log);
            var saveHandler = new SaveHandler(new RequestDelegate(updateHandler.ProcessRequest),sessionFactory,registerSlowMethod, urlBase, log);
            var loadHandler = new LoadHandler(new RequestDelegate(saveHandler.ProcessRequest), sessionFactory, registerSlowMethod, urlBase, log);
            var loadAllHandler = new LoadAllHandler(new RequestDelegate(loadHandler.ProcessRequest),sessionFactory,registerSlowMethod, urlBase, log);
            var staticMethodHandler = new StaticMethodHandler(new RequestDelegate(loadAllHandler.ProcessRequest), sessionFactory, registerSlowMethod, urlBase, log);
            var modelListCallHandler = new ModelListCallHandler(new RequestDelegate(staticMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod, urlBase, log);
            jsHandler = new JSHandler(urlBase,vueImportPath,coreImportPath,new RequestDelegate(modelListCallHandler.ProcessRequest),sessionFactory,registerSlowMethod,compressAllJS,cache,log);
            handlers = new ModelRequestHandlerBase[]
            {
                jsHandler,
                modelListCallHandler,
                staticMethodHandler,
                loadAllHandler,
                loadHandler,
                saveHandler,
                updateHandler,
                deleteHandler,
                instanceMethodHandler
            };
            log?.LogDebug("Starting up VueJS Request Handler");
            methodInstances= new Dictionary<string, SlowMethodInstance>();
            cleanupTimer = new Timer(new TimerCallback(CleanupTimer_Elapsed),null,TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1));
            AssemblyAdded();
        }

        protected string RegisterSlowMethodInstance(string url, InjectableMethod method, object model, object[] pars, IRequestData requestData, ILogger log)
        {
            string ret = $"{url}/{Guid.NewGuid()}".ToLower();
            locker.EnterWriteLock();
            try
            {
                SlowMethodInstance smi = new(method, model, pars, requestData, log);
                methodInstances.Add(ret, smi);
            }
            catch (Exception e)
            {
                log?.LogError("Attempting to register a slow method caused an error. {}", e.Message);
                ret=null;
            }
            locker.ExitWriteLock();
            return ret;
        }
        public bool HandlesJSPath(string url)
            => jsHandler.HandlesJSPath(url);

        private void CleanupTimer_Elapsed(object state)
        {
            locker.EnterWriteLock();
            string[] keys = new string[methodInstances.Count];
            methodInstances.Keys.CopyTo(keys, 0);
            keys.ForEach(key =>
            {
                if (methodInstances[key].IsExpired)
                {
                    SlowMethodInstance smi = methodInstances[key];
                    try { smi.Dispose(); } catch (Exception ex) { log?.LogError("SlowMethodInstance diposal error {}", ex.Message); }
                    methodInstances.Remove(key);
                }
                else if (methodInstances[key].IsFinished)
                    methodInstances.Remove(key);
            });
            locker.ExitWriteLock();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        cleanupTimer.Dispose();
                    }
                    catch (Exception e) { log?.LogError("CleanupTimer disposal error {}",e.Message); }
                    locker.EnterWriteLock();
                    string[] keys = new string[methodInstances.Count];
                    methodInstances.Keys.CopyTo(keys, 0);
                    keys.ForEach(key =>
                    {
                        try { methodInstances[key].Dispose(); } catch (Exception e) { log?.LogError("Method Instance disposal error {}", e.Message); };
                        methodInstances.Remove(key);
                    });
                    locker.ExitWriteLock();
                    locker.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Called to handle a given request
        /// </summary>
        /// <param name="context">The context of the request</param>
        /// <returns>a task as a result of handling the request</returns>
        public override async Task ProcessRequest(HttpContext context)
        {
            log?.LogDebug("Checking if {} is handled by VueJS library", Utility.SantizeLogValue(context.Request.Path));
            if (Enum.TryParse(typeof(RequestMethods), context.Request.Method.ToUpper(), out object method))
            {
                if (context.Request.Method.ToUpper()=="PULL")
                {
                    string url = Utility.CleanURL(Utility.BuildURL(context, urlBase));
                    locker.EnterReadLock();
                    if (!methodInstances.TryGetValue(url.ToLower(), out SlowMethodInstance smi))
                        smi=null;
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
                    }
                    else
                    {
                        if (smi?.IsExpired??false)
                        {
                            locker.EnterWriteLock();
                            methodInstances.Remove(url.ToLower());
                            try { smi.Dispose(); } catch (Exception e) { log?.LogError("SlowMethodInstance disposal error {}", e.Message); }
                            locker.ExitWriteLock();
                        }
                        await next(context);
                    }
                }
                else
                {
                    try
                    {
                        await handlers[0].ProcessRequest(context);
                    }
                    catch (CallNotFoundException cnfe)
                    {
                        log?.LogError("Request Error, call not found: {}",cnfe.Message);
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync(cnfe.Message);
                    }
                    catch (InsecureAccessException iae)
                    {
                        log?.LogError("Request Error, insecure access: {}", iae.Message);
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync(iae.Message);
                    }
                    catch (Exception e)
                    {
                        log?.LogError("Request Error: {}",e.Message);
                        context.Response.ContentType= "text/text";
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Error");
                    }
                }
            }
            else
                await next(context);
        }

        public void UnloadAssemblyContext(string contextName){
            List<Type> types = Utility.UnloadAssemblyContext(contextName);
            if (types!=null)
                handlers.ForEach(mrhb => mrhb.UnloadTypes(types));
        }

        public void AssemblyAdded()
        {
            log?.LogDebug("Assembly added called, rebuilding handlers...");
            isInitialized=false;
            Utility.ClearCaches(log);
            handlers.ForEach(irh =>
            {
                log?.LogDebug("Clearing cache for handler {}", irh.GetType().Name);
                irh.ClearCache();
            });
            log?.LogDebug("Processing all available AssemblyLoadContexts...");
            AssemblyLoadContext.All
                .ForEach(alc => AsssemblyLoadContextAdded(alc));
        }

        public void AsssemblyLoadContextAdded(string contextName)
        {
            var alc = AssemblyLoadContext.All.FirstOrDefault(alc => string.Equals(alc.Name, contextName, StringComparison.InvariantCultureIgnoreCase));
            if (alc!=null)
                AsssemblyLoadContextAdded(alc);
        }

        public void AsssemblyLoadContextAdded(AssemblyLoadContext alc){
            log?.LogDebug("Loading Assembly Load Context {}",  alc.Name);
            List<Exception> errors = DefinitionValidator.Validate(alc, log, out List<Type> invalidModels, out List<Type> models);
            if (!isInitialized)
                this.invalidModels = new();
            this.invalidModels.AddRange(invalidModels);
            if (errors.Count > 0)
            {
                log?.LogError("Validation errors:");
                errors.ForEach(e => log?.LogError("Validation Error: {}", e.Message));
                log?.LogError("Invalid IModels:");
                invalidModels.ForEach(t => log?.LogError("Invalid Model: {}", t.FullName));
            }
            if (errors.Count > 0 && !ignoreInvalidModels)
                throw new ModelValidationException(errors);
            models.RemoveAll(m => this.invalidModels.Contains(m));
            handlers.ForEach(mrhb => mrhb.LoadTypes(models));
            isInitialized=true;
        }
    }
}
