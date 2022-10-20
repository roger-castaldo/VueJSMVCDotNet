using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Handlers;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if !NETSTANDARD && !NET481
using System.Runtime.Loader;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class ModelRequestHandler : IDisposable
    {
        internal enum RequestMethods
        {
            GET,
            PUT,
            DELETE,
            PATCH,
            METHOD,
            SMETHOD,
            PULL
        }

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private List<Type> _invalidModels;
#if NET
        private bool _isInitialized=false;
#endif
        internal bool IsTypeAllowed(Type type)
        {
            return !_invalidModels.Contains(type);
        }

        private Dictionary<Type, ASecurityCheck[]> _typeChecks;
        private Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>> _methodChecks;
        private Dictionary<string,SlowMethodInstance> _methodInstances;
        private Timer _cleanupTimer;
        private string _defaultModelNamespace="App.Models";
        private string _urlBase;
        internal string RegisterSlowMethodInstance(string url,MethodInfo method,object model,object[] pars)
        {
            string ret = (url+"/"+Guid.NewGuid().ToString()).ToLower();
            try
            {
                SlowMethodInstance smi = new SlowMethodInstance(method,model, pars);
                lock (_methodInstances)
                {
                    _methodInstances.Add(ret, smi);
                }
            }catch(Exception e)
            {
                ret=null;
            }
            return ret;
        }

        private IRequestHandler[] _Handlers;

        private static DateTime _startTime;
        internal static DateTime StartTime { get { return _startTime; } }

        private static readonly Regex _baseUrlRegex = new Regex("^(https?:/)?/(.+)(/)$", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase);

        public ModelRequestHandler(ILogWriter logWriter,
            string baseURL)
        {
            _urlBase=baseURL;
            if (_urlBase!=null && !_baseUrlRegex.IsMatch(_urlBase))
            {
                if (!_urlBase.EndsWith("/"))
                    _urlBase+="/";
                if (_urlBase!="/" && !_baseUrlRegex.IsMatch(_urlBase))
                    _urlBase="/"+_urlBase;
            }
            _startTime = DateTime.Now;
            _Handlers = new IRequestHandler[]
            {
                new JSHandler(_defaultModelNamespace,_urlBase),
                new LoadAllHandler(),
                new StaticMethodHandler(this),
                new LoadHandler(),
                new UpdateHandler(),
                new SaveHandler(),
                new DeleteHandler(),
                new InstanceMethodHandler(this),
                new ModelListCallHandler()
            };
            Logger.Setup(logWriter);
            Logger.Debug("Starting up VueJS Request Handler");
            _typeChecks = new Dictionary<Type, ASecurityCheck[]>();
            _methodChecks = new Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>>();
            _methodInstances= new Dictionary<string, SlowMethodInstance>();
            _cleanupTimer = new Timer(5*60*1000);
            _cleanupTimer.Elapsed+=_cleanupTimer_Elapsed;
            _cleanupTimer.Start();
            AssemblyAdded();
        }

        private void _cleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
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
                        try { smi.Dispose(); } catch (Exception ex) { }
                        _methodInstances.Remove(str);
                    }else if (_methodInstances[str].IsFinished)
                    {
                        _methodInstances.Remove(str);
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of the request handler and cleans up the resources there in
        /// </summary>
        public void Dispose()
        {
            try
            {
                _cleanupTimer.Stop();
            }
            catch (Exception e) { }
            try
            {
                _cleanupTimer.Dispose();
            }
            catch (Exception e) { }
            lock (_methodInstances)
            {
                string[] keys = new string[_methodInstances.Count];
                _methodInstances.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    try { _methodInstances[str].Dispose(); } catch (Exception e) { };
                    _methodInstances.Remove(str);
                }
            }
        }

        /// <summary>
        /// Called to check if this class handles the given request
        /// </summary>
        /// <param name="context">The context of the request</param>
        /// <returns>true if the request is handled by the library</returns>
        public bool HandlesRequest(HttpContext context) {
            string url = Utility.CleanURL(Utility.BuildURL(context, _urlBase));
            Logger.Debug("Checking if {0} is handled by VueJS library", new object[] { url });
            object method;
#if !NET481
            if (Enum.TryParse(typeof(RequestMethods), context.Request.Method.ToUpper(), out method))
            {
#else
            RequestMethods rm;
            if (Enum.TryParse<RequestMethods>(context.Request.Method.ToUpper(),out rm))
            {
                method = rm;
#endif
            
                if ((RequestMethods)method==RequestMethods.PULL)
                {
                    bool ret = false;
                    lock (_methodInstances)
                    {
                        ret= _methodInstances.ContainsKey(url.ToLower());
                    }
                    return ret;
                }
                else
                {
                    foreach (IRequestHandler handler in _Handlers)
                    {
                        Logger.Trace("Checking if {0} handles {1}:{2}", new object[] { handler.GetType().FullName, method, url });
                        if (handler.HandlesRequest(url, (RequestMethods)method))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Called to handle a given request
        /// </summary>
        /// <param name="context">The context of the request</param>
        /// <param name="session">The security session associated with the request</param>
        /// <returns>a task as a result of handling the request</returns>
        public async Task ProcessRequest(HttpContext context,ISecureSession session)
        {
            string url = Utility.CleanURL(Utility.BuildURL(context, _urlBase));
            RequestMethods method = (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());
            Logger.Debug("Attempting to handle request {0}:{1}", new object[] { method, url });
            Hashtable formData = new Hashtable();
            if (context.Request.ContentType!=null && 
            (
                context.Request.ContentType=="application/x-www-form-urlencoded" 
                || context.Request.ContentType.StartsWith("multipart/form-data")
            ))
            {
                foreach (string key in context.Request.Form.Keys){
                    Logger.Trace("Loading form data value from key {0}", new object[] { key });
                    if (key.EndsWith(":json")){
                        if (context.Request.Form[key].Count>1){
                            ArrayList al = new ArrayList();
                            foreach (string str in context.Request.Form[key]){
                                al.Add(JSON.JsonDecode(str));
                            }
                            formData.Add(key.Substring(0,key.Length-5),al);
                        }else{
                            formData.Add(key.Substring(0,key.Length-5),JSON.JsonDecode(context.Request.Form[key][0]));
                        }
                    }else{
                        if (context.Request.Form[key].Count>1){
                            ArrayList al = new ArrayList();
                            foreach (string str in context.Request.Form[key]){
                                al.Add(str);
                            }
                            formData.Add(key,al);
                        }else{
                            formData.Add(key,context.Request.Form[key][0]);
                        }
                    }
                }
            }else{
                string tmp =  await new StreamReader(context.Request.Body).ReadToEndAsync();
                if (tmp!=""){
                    Logger.Trace("Loading form data from request body");
                    formData = (Hashtable)JSON.JsonDecode(tmp);
                }
            }
            bool found = false;
            if (method==RequestMethods.PULL)
            {
                SlowMethodInstance smi = null;
                lock (_methodInstances)
                {
                    if (_methodInstances.ContainsKey(url.ToLower()))
                    {
                        smi=_methodInstances[url];
                        if (smi.IsExpired)
                        {
                            _methodInstances.Remove(url.ToLower());
                            smi=null;
                        }
                    }
                }
                if (smi!=null)
                {
                    found=true;
                    await smi.HandleRequest(context);
                    if (smi.IsFinished)
                    {
                        lock (_methodInstances)
                        {
                            _methodInstances.Remove(url.ToLower());
                        }
                    }
                }
            }
            else
            {
                foreach (IRequestHandler handler in _Handlers)
                {
                    if (handler.HandlesRequest(url, method))
                    {
                        found = true;
                        try
                        {
                            await handler.HandleRequest(url, method, formData, context, session, new IsValidCall(_ValidCall));
                        }
                        catch (CallNotFoundException cnfe)
                        {
                            Logger.LogError(cnfe);
                            context.Response.ContentType = "text/text";
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync(cnfe.Message);
                        }
                        catch (InsecureAccessException iae)
                        {
                            Logger.LogError(iae);
                            context.Response.ContentType = "text/text";
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync(iae.Message);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e);
                            context.Response.ContentType= "text/text";
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync("Error");
                        }
                    }
                }
            }
            if (!found)
            {
                context.Response.ContentType = "text/text";
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Not Found");
            }
        }

        private bool _ValidCall(Type t, MethodInfo method, ISecureSession session,IModel model,string url, Hashtable parameters)
        {
            Logger.Trace("Checking security for call {0} under class {1}.{2}", new object[] { url, t.FullName, (method==null ? null :  method.Name) });
            List<ASecurityCheck> checks = new List<ASecurityCheck>();
            lock (_typeChecks)
            {
                if (_typeChecks.ContainsKey(t))
                    checks.AddRange(_typeChecks[t]);
            }
            if (method != null)
            {
                lock (_methodChecks)
                {
                    if (_methodChecks.ContainsKey(t))
                    {
                        if (_methodChecks[t].ContainsKey(method))
                            checks.AddRange(_methodChecks[t][method]);
                    }
                }
            }
            foreach (ASecurityCheck asc in checks)
            {
                if (!asc.HasValidAccess(session,model,url,parameters))
                    return false;
            }
            return true;
        }

#if NET
        /// <summary>
        /// called when an assemblyloadcontext needs to be unloaded, this will remove all references to 
        /// that load context to allow for an unload
        /// </summary>
        /// <param name="context">The assembly context being unloaded</param>
        public void UnloadAssemblyContext(AssemblyLoadContext context){
            UnloadAssemblyContext(context.Name);
        }
        /// <summary>
        /// called when an assembly context needs to be unloaded without providing the context but its name
        /// instead
        /// </summary>
        /// <param name="contextName">The name of the assembly load context to unload</param>
        public void UnloadAssemblyContext(string contextName){
            List<Type> types = Utility.UnloadAssemblyContext(contextName);
            if (types!=null){
                foreach (IRequestHandler irh in _Handlers)
                    irh.UnloadTypes(types);
                lock (_typeChecks)
                {
                    foreach (Type t in types){
                        _typeChecks.Remove(t);
                    }
                }
                lock (_methodChecks)
                {
                    foreach (Type t in types){
                        _methodChecks.Remove(t);
                    }
                }
            }
        }

        /// <summary>
        /// called when a new assembly has been loaded in the case of dynamic loading, in order 
        /// to rescan for all new model types and add them accordingly.
        /// </summary>
        public void AssemblyAdded()
        {
            Logger.Debug("Assembly added called, rebuilding handlers...");
            _isInitialized=false;
            Utility.ClearCaches();
            foreach (IRequestHandler irh in _Handlers)
            {
                Logger.Debug("Clearing cache for handler {0}",new object[] { irh.GetType().Name });
                irh.ClearCache();
            }
            Logger.Debug("Processing all available AssemblyLoadContexts...");
            foreach (AssemblyLoadContext alc in AssemblyLoadContext.All){
                AsssemblyLoadContextAdded(alc);
            }
        }

        /// <summary>
        /// Called when a new Assembly Load Context has been added
        /// </summary>
        /// <param name="contextName">The name of the context that was added</param>
        public void AsssemblyLoadContextAdded(string contextName){
            foreach (AssemblyLoadContext alc in AssemblyLoadContext.All){
                if (alc.Name==contextName){
                    AsssemblyLoadContextAdded(alc);
                    break;
                }
            }
        }

        /// <summary>
        /// Called when a new Assembly Load Context has been added
        /// </summary>
        /// <param name="alc">The assembly load context that was added</param>
        /// <exception cref="ModelValidationException">Houses a set of exceptions if any newly loaded models fail validation</exception>
        public void AsssemblyLoadContextAdded(AssemblyLoadContext alc){
            Logger.Debug("Loading Assembly Load Context {0}", new object[] { alc.Name });
            List<Type> models;
            List<Type> invalidModels;
            List<Exception> errors = DefinitionValidator.Validate(alc,out invalidModels,out models);
            if (!_isInitialized)
                _invalidModels = new List<Type>();
            _invalidModels.AddRange(invalidModels);
            if (errors.Count > 0)
            {
                Logger.Error("Validation errors:");
                foreach (Exception e in errors)
                    Logger.LogError(e);
                Logger.Error("Invalid IModels:");
                foreach (Type t in _invalidModels)
                    Logger.Error(t.FullName);
            }
            if (errors.Count > 0)
                throw new ModelValidationException(errors);
            for(int x = 0; x < models.Count; x++)
            {
                if (_invalidModels.Contains(models[x]))
                {
                    models.RemoveAt(x);
                    x--;
                }
            }
            foreach (IRequestHandler irh in _Handlers){
                if (_isInitialized)
                    irh.LoadTypes(models);
                else
                    irh.Init(models);
            }
            lock (_typeChecks)
            {
                foreach (Type t in models)
                {
                    if (IsTypeAllowed(t))
                    {
                        List<ASecurityCheck> checks = new List<ASecurityCheck>();
                        foreach (object obj in t.GetCustomAttributes())
                        {
                            if (obj is ASecurityCheck)
                                checks.Add((ASecurityCheck)obj);
                        }
                        _typeChecks.Add(t,checks.ToArray());
                        Dictionary<MethodInfo, ASecurityCheck[]> methodChecks = new Dictionary<MethodInfo, ASecurityCheck[]>();
                        foreach (MethodInfo mi in t.GetMethods())
                        {
                            checks = new List<ASecurityCheck>();
                            foreach (object obj in mi.GetCustomAttributes())
                            {
                                if (obj is ASecurityCheck)
                                    checks.Add((ASecurityCheck)obj);
                            }
                            methodChecks.Add(mi, checks.ToArray());
                        }
                        _methodChecks.Add(t, methodChecks);
                    }
                }
            }
            _isInitialized=true;
        }
#else
        ///<summary>
        ///called when a new assembly has been loaded in the case of dynamic loading, in order 
        ///to rescan for all new model types and add them accordingly.
        ///</summary>
        public void AssemblyAdded()
        {
            Utility.ClearCaches();
            foreach (IRequestHandler irh in _Handlers)
                irh.ClearCache();
            List<Type> models;
            List<Exception> errors = DefinitionValidator.Validate(out _invalidModels,out models);
            if (errors.Count > 0)
            {
                Logger.Error("Backbone validation errors:");
                foreach (Exception e in errors)
                    Logger.LogError(e);
                Logger.Error("Invalid IModels:");
                foreach (Type t in _invalidModels)
                    Logger.Error(t.FullName);
            }
            if (errors.Count > 0)
                throw new ModelValidationException(errors);
            for(int x = 0; x < models.Count; x++)
            {
                if (_invalidModels.Contains(models[x]))
                {
                    models.RemoveAt(x);
                    x--;
                }
            }
            foreach (IRequestHandler irh in _Handlers)
                irh.Init(models);
            lock (_typeChecks)
            {
                _typeChecks.Clear();
                _methodChecks.Clear();
                foreach (Type t in models)
                {
                    if (IsTypeAllowed(t))
                    {
                        List<ASecurityCheck> checks = new List<ASecurityCheck>();
                        foreach (object obj in t.GetCustomAttributes())
                        {
                            if (obj is ASecurityCheck)
                                checks.Add((ASecurityCheck)obj);
                        }
                        _typeChecks.Add(t,checks.ToArray());
                        Dictionary<MethodInfo, ASecurityCheck[]> methodChecks = new Dictionary<MethodInfo, ASecurityCheck[]>();
                        foreach (MethodInfo mi in t.GetMethods())
                        {
                            checks = new List<ASecurityCheck>();
                            foreach (object obj in mi.GetCustomAttributes())
                            {
                                if (obj is ASecurityCheck)
                                    checks.Add((ASecurityCheck)obj);
                            }
                            methodChecks.Add(mi, checks.ToArray());
                        }
                        _methodChecks.Add(t, methodChecks);
                    }
                }
            }
        }
#endif
    }
}
