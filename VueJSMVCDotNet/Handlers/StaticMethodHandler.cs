using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            public string HandleRequest(string url, string formData, out string contentType, out int responseStatus)
            {
                Match m = _reg.Match(url);
                string method = m.Groups[1].Value;
                MethodInfo mi;
                object[] pars;
                Utility.LocateMethod(formData, _methods[method], out mi, out pars);
                if (mi == null)
                {
                    contentType = "text/text";
                    responseStatus = 404;
                    return "Unable to locate requested method to invoke";
                }
                else
                {
                    contentType = "text/json";
                    responseStatus = 200;
                    try
                    {
                        if (mi.ReturnType == typeof(void))
                        {
                            mi.Invoke(null,pars);
                            return "";
                        }
                        else
                            return JSON.JsonEncode(mi.Invoke(null,pars));
                    }
                    catch (Exception ex)
                    {
                        contentType = "text/text";
                        responseStatus = 500;
                        return "Execution Error";
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

        public string HandleRequest(string url, RequestHandler.RequestMethods method, string formData, out string contentType, out int responseStatus)
        {
            contentType = "text/text";
            responseStatus = 404;
            lock (_patterns)
            {
                foreach (sMethodPatterns smp in _patterns)
                {
                    if (smp.IsValid(url))
                        return smp.HandleRequest(url, formData, out contentType, out responseStatus);
                }
            }
            return "Not Found";
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
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
                foreach (Type t in types)
                {
                    List<MethodInfo> methods = new List<MethodInfo>();
                    foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                            methods.Add(mi);
                    }
                    _patterns.Add(new sMethodPatterns(Utility.GetModelUrlRoot(t), methods));
                }
            }
        }
    }
}
