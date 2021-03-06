﻿using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Handlers;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    public class RequestHandler
    {
        //how to startup the system as per their names, either disable invalid models or throw 
        //and exception about them
        public enum StartTypes
        {
            DisableInvalidModels,
            ThrowInvalidExceptions
        }

        internal enum RequestMethods
        {
            GET,
            PUT,
            DELETE,
            PATCH,
            METHOD,
            SMETHOD
        }

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private List<Type> _invalidModels;
        internal bool IsTypeAllowed(Type type)
        {
            return !_invalidModels.Contains(type);
        }
        private StartTypes _startType = StartTypes.DisableInvalidModels;

        private Dictionary<Type, ASecurityCheck[]> _typeChecks;
        private Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>> _methodChecks;

        private IRequestHandler[] _Handlers = new IRequestHandler[]
        {
            new JSHandler(),
            new LoadAllHandler(),
            new StaticMethodHandler(),
            new LoadHandler(),
            new UpdateHandler(),
            new SaveHandler(),
            new DeleteHandler(),
            new InstanceMethodHandler(),
            new ModelListCallHandler()
        };

        private static DateTime _startTime;
        internal static DateTime StartTime { get { return _startTime; } }

        public RequestHandler(StartTypes startType,ILogWriter logWriter)
        {
            _startTime = DateTime.Now;
            Logger.Setup(logWriter);
            Logger.Debug("Starting up VueJS Request Handler");
            _startType = startType;
            _typeChecks = new Dictionary<Type, ASecurityCheck[]>();
            _methodChecks = new Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>>();
            AssemblyAdded();
        }

        public bool HandlesRequest(HttpContext context) {
            string url = Utility.CleanURL(Utility.BuildURL(context));
            object method;
            if (Enum.TryParse(typeof(RequestMethods), context.Request.Method.ToUpper(), out method))
            {
                foreach (IRequestHandler handler in _Handlers)
                {
                    if (handler.HandlesRequest(url, (RequestMethods)method))
                        return true;
                }
            }
            return false;
        }

        public async Task ProcessRequest(HttpContext context,ISecureSession session)
        {
            string url = Utility.CleanURL(Utility.BuildURL(context));
            RequestMethods method = (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());
            Hashtable formData = new Hashtable();
            if (context.Request.ContentType!=null && 
            (
                context.Request.ContentType=="application/x-www-form-urlencoded" 
                || context.Request.ContentType.StartsWith("multipart/form-data")
            ))
            {
                foreach (string key in context.Request.Form.Keys){
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
                    formData = (Hashtable)JSON.JsonDecode(tmp);
                }
            }
            bool found = false;
            foreach (IRequestHandler handler in _Handlers)
            {
                if (handler.HandlesRequest(url, method))
                {
                    found = true;
                    try
                    {
                        await handler.HandleRequest(url, method, formData, context,session,new IsValidCall(_ValidCall));
                    }
                    catch(CallNotFoundException cnfe)
                    {
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync(cnfe.Message);
                    }
                    catch (InsecureAccessException iae)
                    {
                        context.Response.ContentType = "text/text";
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync(iae.Message);
                    }
                    catch (Exception e)
                    {
                        context.Response.ContentType= "text/text";
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Error");
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

        //called when a new assembly has been loaded in the case of dynamic loading, in order 
        //to rescan for all new model types and add them accordingly.
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
            if (_startType == StartTypes.ThrowInvalidExceptions && errors.Count > 0)
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
    }
}
