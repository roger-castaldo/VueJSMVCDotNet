using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using VueJSMVCDotNet.Handlers.Model;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers
{
    internal class ModelRequestHandler : RequestHandlerBase
    {
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
        private List<Type> _invalidModels;
        private bool _isInitialized=false;
        private readonly Dictionary<string, SlowMethodInstance> _methodInstances;
        private readonly Timer _cleanupTimer;
        protected string RegisterSlowMethodInstance(string url,InjectableMethod method,object model,object[] pars,IRequestData requestData,ILogger log)
        {
            string ret = (url+"/"+Guid.NewGuid().ToString()).ToLower();
            try
            {
                SlowMethodInstance smi = new(method,model, pars,requestData,log);
                lock (_methodInstances)
                {
                    _methodInstances.Add(ret, smi);
                }
            }catch(Exception e)
            {
                log?.LogError("Attempting to register a slow method caused an error. {}",e.Message);
                ret=null;
            }
            return ret;
        }

        private readonly ModelRequestHandlerBase[] _Handlers;

        private static readonly DateTime _startTime = DateTime.Now;
        internal static DateTime StartTime { get { return _startTime; } }
        
        private readonly string _urlBase;
        private readonly bool _ignoreInvalidModels;

        private static readonly Regex _baseUrlRegex = new("^(https?:/)?/(.+)(/)$", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase,TimeSpan.FromMilliseconds(500));

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
            _urlBase=baseURL;
            _ignoreInvalidModels=ignoreInvalidModels;
            vueImportPath??="https://unpkg.com/vue@3/dist/vue.esm-browser.js";
            if (_urlBase!=null && !_baseUrlRegex.IsMatch(_urlBase))
            {
                if (!_urlBase.EndsWith("/"))
                    _urlBase+="/";
                if (_urlBase!="/" && !_baseUrlRegex.IsMatch(_urlBase))
                    _urlBase="/"+_urlBase;
            }
            var registerSlowMethod = new delRegisterSlowMethodInstance(RegisterSlowMethodInstance);
            var instanceMethodHandler = new InstanceMethodHandler(next, sessionFactory, registerSlowMethod, _urlBase,log);
            var deleteHandler = new DeleteHandler(new RequestDelegate(instanceMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod,_urlBase, log);
            var updateHandler = new UpdateHandler(new RequestDelegate(deleteHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase, log);
            var saveHandler = new SaveHandler(new RequestDelegate(updateHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase, log);
            var loadHandler = new LoadHandler(new RequestDelegate(saveHandler.ProcessRequest), sessionFactory, registerSlowMethod, _urlBase, log);
            var loadAllHandler = new LoadAllHandler(new RequestDelegate(loadHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase, log);
            var staticMethodHandler = new StaticMethodHandler(new RequestDelegate(loadAllHandler.ProcessRequest), sessionFactory, registerSlowMethod, _urlBase, log);
            var modelListCallHandler = new ModelListCallHandler(new RequestDelegate(staticMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase, log);
            var jsHandler = new JSHandler(_urlBase,vueImportPath,coreImportPath,new RequestDelegate(modelListCallHandler.ProcessRequest),sessionFactory,registerSlowMethod,compressAllJS,cache,log);
            _Handlers = new ModelRequestHandlerBase[]
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
            _methodInstances= new Dictionary<string, SlowMethodInstance>();
            _cleanupTimer = new Timer(new TimerCallback(CleanupTimer_Elapsed),null,TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1));
            AssemblyAdded();
        }

        private void CleanupTimer_Elapsed(object state)
        {
            lock (_methodInstances)
            {
                string[] keys = new string[_methodInstances.Count];
                _methodInstances.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (_methodInstances[str].IsExpired)
                    {
                        SlowMethodInstance smi = _methodInstances[str];
                        try { smi.Dispose(); } catch (Exception ex) { log?.LogError("SlowMethodInstance diposal error {}",ex.Message); }
                        _methodInstances.Remove(str);
                    }else if (_methodInstances[str].IsFinished)
                    {
                        _methodInstances.Remove(str);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _cleanupTimer.Dispose();
                    }
                    catch (Exception e) { log?.LogError("CleanupTimer disposal error {}",e.Message); }
                    lock (_methodInstances)
                    {
                        string[] keys = new string[_methodInstances.Count];
                        _methodInstances.Keys.CopyTo(keys, 0);
                        foreach (string str in keys)
                        {
                            try { _methodInstances[str].Dispose(); } catch (Exception e) { log?.LogError("Method Instance disposal error {}",e.Message); };
                            _methodInstances.Remove(str);
                        }
                    }
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
            log?.LogDebug("Checking if {} is handled by VueJS library", context.Request.Path);
            if (Enum.TryParse(typeof(RequestMethods), context.Request.Method.ToUpper(), out object method))
            {
                if (context.Request.Method.ToUpper()=="PULL")
                {
                    SlowMethodInstance smi = null;
                    string url = Utility.CleanURL(Utility.BuildURL(context, _urlBase));
                    lock (_methodInstances)
                    {
                        if (_methodInstances.ContainsKey(url.ToLower()))
                        {
                            smi=_methodInstances[url];
                            if (smi.IsExpired)
                            {
                                _methodInstances.Remove(url.ToLower());
                                try { smi.Dispose(); } catch (Exception e) { log?.LogError("SlowMethodInstance disposal error {}",e.Message); }
                                smi=null;
                            }
                        }
                    }
                    if (smi!=null)
                    {
                        await smi.HandleRequest(context);
                        if (smi.IsFinished)
                        {
                            lock (_methodInstances)
                            {
                                _methodInstances.Remove(url.ToLower());
                            }
                        }
                    }
                    else
                        await next(context);
                }
                else
                {
                    try
                    {
                        await _Handlers[0].ProcessRequest(context);
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
            if (types!=null){
                foreach (ModelRequestHandlerBase mrhb in _Handlers)
                    mrhb.UnloadTypes(types);
            }
        }

        public void AssemblyAdded()
        {
            log?.LogDebug("Assembly added called, rebuilding handlers...");
            _isInitialized=false;
            Utility.ClearCaches(log);
            foreach (ModelRequestHandlerBase irh in _Handlers)
            {
                log?.LogDebug("Clearing cache for handler {}",irh.GetType().Name);
                irh.ClearCache();
            }
            log?.LogDebug("Processing all available AssemblyLoadContexts...");
            foreach (AssemblyLoadContext alc in AssemblyLoadContext.All){
                AsssemblyLoadContextAdded(alc);
            }
        }

        public void AsssemblyLoadContextAdded(string contextName){
            foreach (AssemblyLoadContext alc in AssemblyLoadContext.All){
                if (alc.Name==contextName){
                    AsssemblyLoadContextAdded(alc);
                    break;
                }
            }
        }

        public void AsssemblyLoadContextAdded(AssemblyLoadContext alc){
            log?.LogDebug("Loading Assembly Load Context {}",  alc.Name);
            List<Exception> errors = DefinitionValidator.Validate(alc, log, out List<Type> invalidModels, out List<Type> models);
            if (!_isInitialized)
                _invalidModels = new();
            _invalidModels.AddRange(invalidModels);
            if (errors.Count > 0)
            {
                log?.LogError("Validation errors:");
                foreach (Exception e in errors)
                    log?.LogError("Validation Error: {}",e.Message);
                log?.LogError("Invalid IModels:");
                foreach (Type t in _invalidModels)
                    log?.LogError("Invalid Model: {}",t.FullName);
            }
            if (errors.Count > 0 && !_ignoreInvalidModels)
                throw new ModelValidationException(errors);
            models.RemoveAll(m => _invalidModels.Contains(m));
            foreach (ModelRequestHandlerBase mrhb in _Handlers){
                mrhb.LoadTypes(models);
            }
            _isInitialized=true;
        }
    }
}
