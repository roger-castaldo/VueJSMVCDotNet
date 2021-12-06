using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class StaticMethodHandler : IRequestHandler
    {
        private struct sMethodPatterns
        {
            private Regex _reg;
            private Dictionary<string, List<MethodInfo>> _methods;

            public sMethodPatterns(string baseURL,List<MethodInfo> functions)
            {
                _methods = new Dictionary<string, List<MethodInfo>>();
                StringBuilder sb = new StringBuilder();
                foreach (MethodInfo mi in functions)
                {
                    List<MethodInfo> methods = new List<MethodInfo>();
                    if (_methods.ContainsKey(mi.Name))
                    {
                        methods = _methods[mi.Name];
                        _methods.Remove(mi.Name);
                    }
                    else
                        sb.AppendFormat("{0}{1}", new object[] { (sb.Length > 0 ? "|" : ""), mi.Name });
                    methods.Add(mi);
                    _methods.Add(mi.Name, methods);
                }
                _reg = new Regex(string.Format("^{0}/({1})$", new object[] { baseURL, sb.ToString() }), RegexOptions.Compiled | RegexOptions.ECMAScript);
            }

            #if NETCOREAPP3_1
            public bool IsForType(Type type){
                foreach (string str in _methods.Keys){
                    foreach (MethodInfo mi in _methods[str]){
                        if (mi.DeclaringType==type){
                            return true;
                        }
                    }
                }
                return false;
            }
            #endif

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            public Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
            {
                Logger.Trace("Attempting to handle call {0}:{1} with a static method handler", new object[] { method, url });
                Match m = _reg.Match(url);
                string smethod = m.Groups[1].Value;
                MethodInfo mi;
                object[] pars;
                Logger.Trace("Attempting to locate method to handle the static method call at {0}:{1}", new object[] { method, url });
                Utility.LocateMethod(formData, _methods[smethod],session, out mi, out pars);
                if (mi == null)
                    throw new CallNotFoundException("Unable to locate requested method to invoke");
                else
                {
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session,null,url,formData))
                        throw new InsecureAccessException();
                    try
                    {
                        Logger.Trace("Attempting to call the method {0}.{1} to answer the static method call {2}:{3}", new object[] { mi.DeclaringType.Name, mi.Name, method, url });
                        if (mi.ReturnType == typeof(void))
                        {
                            mi.Invoke(null,pars);
                            context.Response.ContentType = "text/json";
                            context.Response.StatusCode = 200;
                            return context.Response.WriteAsync("");
                        }else if (mi.ReturnType==typeof(string)){
                            context.Response.ContentType = "text/text";
                            context.Response.StatusCode = 200;
                            return context.Response.WriteAsync((string)mi.Invoke(null,pars));
                        }
                        else{
                            context.Response.ContentType = "text/json";
                            context.Response.StatusCode = 200;
                            return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(null,pars)));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Execution Error",ex);
                    }
                }
            }
        }

        private List<sMethodPatterns> _patterns;

        public StaticMethodHandler()
        {
            _patterns = new List<sMethodPatterns>();
        }

        public void ClearCache()
        {
            _patterns.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            sMethodPatterns? patt = null;
            lock (_patterns)
            {
                foreach (sMethodPatterns smp in _patterns)
                {
                    if (smp.IsValid(url))
                    {
                        patt = smp;
                        break;
                    }
                }
            }
            if (patt.HasValue)
                return patt.Value.HandleRequest(url, method, formData, context, session, securityCheck);
            throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            Logger.Trace("Checking to see if the request {0}:{1} is handled by the static method handler", new object[] { method, url });
            bool ret = false;
            if (method == RequestHandler.RequestMethods.SMETHOD)
            {
                lock (_patterns)
                {
                    foreach (sMethodPatterns smp in _patterns)
                    {
                        if (smp.IsValid(url))
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        public void Init(List<Type> types)
        {
            lock (_patterns)
            {
                _patterns.Clear();
                _LoadTypes(types);
            }
        }

        private void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                List<MethodInfo> methods = new List<MethodInfo>();
                foreach (MethodInfo mi in t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                        methods.Add(mi);
                }
                _patterns.Add(new sMethodPatterns(Utility.GetModelUrlRoot(t), methods));
            }
        }

        #if NETCOREAPP3_1
        public void LoadTypes(List<Type> types){
            lock(_patterns){
                _LoadTypes(types);
            }
        }
        public void UnloadTypes(List<Type> types){
            lock(_patterns){
                foreach(Type t in types){
                    for(int x=0;x<_patterns.Count;x++){
                        if (_patterns[x].IsForType(t)){
                            _patterns.RemoveAt(x);
                            x--;
                        }
                    }
                }
            }
        }
        #endif
    }
}
