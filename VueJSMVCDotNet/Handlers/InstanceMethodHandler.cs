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
    internal class InstanceMethodHandler : IRequestHandler
    {
        private struct sMethodPatterns
        {
            private Regex _reg;
            private MethodInfo _loadMethod;
            private Dictionary<string, List<MethodInfo>> _methods;

            public sMethodPatterns(string baseURL,MethodInfo loadMethod,List<MethodInfo> functions)
            {
                _loadMethod = loadMethod;
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
                _reg = new Regex(string.Format("^{0}/([^/]+)/({1})$", new object[] { baseURL, sb.ToString() }), RegexOptions.Compiled | RegexOptions.ECMAScript);
            }

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
            {
                Match m = _reg.Match(url);
                string id = m.Groups[1].Value;
                string smethod = m.Groups[2].Value;
                IModel model = (IModel)_loadMethod.Invoke(null, new object[] { id });
                if (model == null)
                    throw new CallNotFoundException("Model Not Found");
                MethodInfo mi;
                object[] pars;
                Utility.LocateMethod(formData, _methods[smethod], out mi, out pars);
                if (mi == null)
                    throw new CallNotFoundException("Unable to locate requested method to invoke");
                else
                {
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session,model,url,(Hashtable)JSON.JsonDecode(formData)))
                        throw new InsecureAccessException();
                    context.Response.ContentType= "text/json";
                    context.Response.StatusCode= 200;
                    try
                    {
                        if (mi.ReturnType == typeof(void))
                        {
                            mi.Invoke(model,pars);
                            return context.Response.WriteAsync("");
                        }
                        else
                            return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(model,pars)));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Execution Error");
                    }
                }
            }
        }

        private List<sMethodPatterns> _patterns;

        public InstanceMethodHandler()
        {
            _patterns = new List<sMethodPatterns>();
        }

        public void ClearCache()
        {
            _patterns.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
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
            bool ret = false;
            if (method == RequestHandler.RequestMethods.METHOD)
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
                foreach (Type t in types)
                {
                    List<MethodInfo> methods = new List<MethodInfo>();
                    MethodInfo loadMethod = null;
                    foreach (MethodInfo m in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                    {
                        if (m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        {
                            loadMethod = m;
                            break;
                        }
                    }
                    if (loadMethod != null)
                    {
                        foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                        {
                            if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                                methods.Add(mi);
                        }
                        if (methods.Count>0)
                            _patterns.Add(new sMethodPatterns(Utility.GetModelUrlRoot(t), loadMethod, methods));
                    }
                }
            }
        }
    }
}
