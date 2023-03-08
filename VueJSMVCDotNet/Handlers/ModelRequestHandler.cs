﻿using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Handlers;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
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

        public delegate string delRegisterSlowMethodInstance(string url, InjectableMethod method, object model, object[] pars, ISecureSession session);

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private List<Type> _invalidModels;
        private bool _isInitialized=false;
        private Dictionary<string,SlowMethodInstance> _methodInstances;
        private readonly Timer _cleanupTimer;
        protected string _RegisterSlowMethodInstance(string url,InjectableMethod method,object model,object[] pars,ISecureSession session)
        {
            string ret = (url+"/"+Guid.NewGuid().ToString()).ToLower();
            try
            {
                SlowMethodInstance smi = new SlowMethodInstance(method,model, pars,session);
                lock (_methodInstances)
                {
                    _methodInstances.Add(ret, smi);
                }
            }catch(Exception e)
            {
                Logger.LogError(e);
                ret=null;
            }
            return ret;
        }

        private readonly ModelRequestHandlerBase[] _Handlers;

        private static readonly DateTime _startTime = DateTime.Now;
        internal static DateTime StartTime { get { return _startTime; } }
        
        private readonly string _urlBase;
        private readonly bool _ignoreInvalidModels;

        private static readonly Regex _baseUrlRegex = new Regex("^(https?:/)?/(.+)(/)$", RegexOptions.Compiled|RegexOptions.ECMAScript|RegexOptions.IgnoreCase);

        public ModelRequestHandler(ILogWriter logWriter,
            string baseURL,
            bool ignoreInvalidModels,
            string vueImportPath,
            string coreJSURL, 
            string coreImportPath,
            string[] securityHeaders,
            ISecureSessionFactory sessionFactory,
            RequestDelegate next)
            : base(next)
        {
            _urlBase=baseURL;
            _ignoreInvalidModels=ignoreInvalidModels;
            vueImportPath=(vueImportPath==null ? "https://unpkg.com/vue@3/dist/vue.esm-browser.js" : vueImportPath);
            if (_urlBase!=null && !_baseUrlRegex.IsMatch(_urlBase))
            {
                if (!_urlBase.EndsWith("/"))
                    _urlBase+="/";
                if (_urlBase!="/" && !_baseUrlRegex.IsMatch(_urlBase))
                    _urlBase="/"+_urlBase;
            }
            var registerSlowMethod = new delRegisterSlowMethodInstance(_RegisterSlowMethodInstance);
            var instanceMethodHandler = new InstanceMethodHandler(next, sessionFactory, registerSlowMethod, _urlBase);
            var deleteHandler = new DeleteHandler(new RequestDelegate(instanceMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod,_urlBase);
            var updateHandler = new UpdateHandler(new RequestDelegate(deleteHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase);
            var saveHandler = new SaveHandler(new RequestDelegate(updateHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase);
            var loadHandler = new LoadHandler(new RequestDelegate(saveHandler.ProcessRequest), sessionFactory, registerSlowMethod, _urlBase);
            var loadAllHandler = new LoadAllHandler(new RequestDelegate(loadHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase);
            var staticMethodHandler = new StaticMethodHandler(new RequestDelegate(loadAllHandler.ProcessRequest), sessionFactory, registerSlowMethod, _urlBase);
            var modelListCallHandler = new ModelListCallHandler(new RequestDelegate(staticMethodHandler.ProcessRequest),sessionFactory,registerSlowMethod, _urlBase);
            var jsHandler = new JSHandler(_urlBase,vueImportPath,coreJSURL,coreImportPath,securityHeaders,new RequestDelegate(modelListCallHandler.ProcessRequest),sessionFactory,registerSlowMethod);
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
            Logger.Setup(logWriter);
            Logger.Debug("Starting up VueJS Request Handler");
            _methodInstances= new Dictionary<string, SlowMethodInstance>();
            _cleanupTimer = new Timer(60*1000);
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
                        try { smi.Dispose(); } catch (Exception ex) { Logger.LogError(ex); }
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
        public override void Dispose()
        {
            try
            {
                _cleanupTimer.Stop();
            }
            catch (Exception e) { Logger.LogError(e); }
            try
            {
                _cleanupTimer.Dispose();
            }
            catch (Exception e) { Logger.LogError(e); }
            lock (_methodInstances)
            {
                string[] keys = new string[_methodInstances.Count];
                _methodInstances.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    try { _methodInstances[str].Dispose(); } catch (Exception e) { Logger.LogError(e); };
                    _methodInstances.Remove(str);
                }
            }
        }

        /// <summary>
        /// Called to handle a given request
        /// </summary>
        /// <param name="context">The context of the request</param>
        /// <returns>a task as a result of handling the request</returns>
        public override async Task ProcessRequest(HttpContext context)
        {
            Logger.Debug("Checking if {0} is handled by VueJS library", new object[] { context.Request.Path });
            object method;
            if (Enum.TryParse(typeof(RequestMethods), context.Request.Method.ToUpper(), out method))
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
                                try { smi.Dispose(); }catch(Exception e) { Logger.LogError(e);}
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
                        await _next(context);
                }
                else
                {
                    try
                    {
                        await _Handlers[0].ProcessRequest(context);
                    }
                    catch (CallNotFoundException cnfe)
                    {
                        Logger.LogError(cnfe);
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 404;
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
            else
                await _next(context);
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
            Logger.Debug("Assembly added called, rebuilding handlers...");
            _isInitialized=false;
            Utility.ClearCaches();
            foreach (ModelRequestHandlerBase irh in _Handlers)
            {
                Logger.Debug("Clearing cache for handler {0}",new object[] { irh.GetType().Name });
                irh.ClearCache();
            }
            Logger.Debug("Processing all available AssemblyLoadContexts...");
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
