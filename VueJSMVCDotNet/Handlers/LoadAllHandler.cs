﻿using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class LoadAllHandler : IRequestHandler
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadAllHandler()
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _methods.Clear();
        }

        public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            Logger.Trace("Attempting to handle {0}:{1} inside the Load All Handler", new object[] { method, url });
            MethodInfo mi = null;
            lock (_methods)
            {
                if (_methods.ContainsKey(url))
                    mi = _methods[url];
            }
            if (mi != null)
            {
                if (!securityCheck.Invoke(mi.DeclaringType, mi, session,null,url,null))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode = 200;
                Logger.Trace("Invoking the Load All call {0}.{1} to handle {2}:{3}", new object[] { mi.DeclaringType.FullName, mi.Name, method, url });
                return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(null, (mi.GetParameters().Length==1 ? new object[]{session} : new object[] { }))));
            }
            else
                throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method)
        {
            Logger.Trace("Checking if {0}:{1} is handled by the Load All Handler", new object[] { method, url });
            if (method==ModelRequestHandler.RequestMethods.GET)
                return _methods.ContainsKey(url);
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_methods)
            {
                _methods.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                    {
                        _methods.Add(Utility.GetModelUrlRoot(t), mi);
                        break;
                    }
                }
            }
        }

        #if NET

        public void LoadTypes(List<Type> types){
            lock(_methods){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            lock(_methods){
                string[] keys = new string[_methods.Count];
                _methods.Keys.CopyTo(keys,0);
                foreach (string str in keys){
                    if (types.Contains(_methods[str].DeclaringType)){
                        _methods.Remove(str);
                    }
                }
            }
        }
        #endif
    }
}
