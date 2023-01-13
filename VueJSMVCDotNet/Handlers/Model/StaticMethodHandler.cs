using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class StaticMethodHandler : ModelRequestHandlerBase
    {
        private struct sMethodPatterns
        {
            private Regex _reg;
            private Dictionary<string, List<MethodInfo>> _methods;
            private List<MethodInfo> _slowMethods;
            public List<MethodInfo> SlowMethods => _slowMethods;

            public sMethodPatterns(string baseURL,List<MethodInfo> functions)
            {
                _methods = new Dictionary<string, List<MethodInfo>>();
                _slowMethods = new List<MethodInfo>();
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
                    if (((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).IsSlow)
                        _slowMethods.Add(mi);
                    _methods.Add(mi.Name, methods);
                }
                _reg = new Regex(string.Format("^{0}/({1})$", new object[] { baseURL, sb.ToString() }), RegexOptions.Compiled | RegexOptions.ECMAScript);
            }

            public List<MethodInfo> GetMethods(string url)
            {
                Match m = _reg.Match(url);
                string smethod = m.Groups[1].Value;
                return _methods[smethod];
            }

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

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }
        }

        private List<sMethodPatterns> _patterns;
        private ModelRequestHandler _handler;

        public StaticMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            : base(next,sessionFactory, registerSlowMethod, urlBase)
        {
            _patterns = new List<sMethodPatterns>();
        }

        public override void ClearCache()
        {
            _patterns.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            Logger.Trace("Checking to see if the request {0}:{1} is handled by the static method handler", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)==RequestMethods.SMETHOD)
            {
                sMethodPatterns? patterns = null;
                lock (_patterns)
                {
                    patterns = _patterns.Where(p => p.IsValid(url)).FirstOrDefault();
                }
                if (patterns != null)
                {
                    Logger.Trace("Attempting to handle call {0}:{1} with a static method handler", new object[] { GetRequestMethod(context), url });
                    sRequestData requestData = await _ExtractParts(context);
                    List<MethodInfo> methods = patterns.Value.GetMethods(url);
                    MethodInfo mi;
                    object[] pars;
                    Logger.Trace("Attempting to locate method to handle the static method call at {0}:{1}", new object[] { GetRequestMethod(context), url });
                    Utility.LocateMethod(requestData.FormData, methods, requestData.Session, out mi, out pars);
                    if (mi == null)
                        throw new CallNotFoundException("Unable to locate requested method to invoke");
                    else
                    {
                        if (!await _ValidCall(mi.DeclaringType,mi,null,context))
                            throw new InsecureAccessException();
                        try
                        {
                            Logger.Trace("Attempting to call the method {0}.{1} to answer the static method call {2}:{3}", new object[] { mi.DeclaringType.Name, mi.Name, GetRequestMethod(context), url });
                            if (patterns.Value.SlowMethods.Contains(mi))
                            {
                                string newPath = _RegisterSlowMethodInstance(url, mi, null, pars, requestData.Session);
                                if (newPath!= null)
                                {
                                    context.Response.ContentType = "text/json";
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync(JSON.JsonEncode(newPath));
                                    return;
                                }
                                else
                                    throw new Exception("Execution Error");
                            }
                            else
                            {
                                if (mi.ReturnType == typeof(void))
                                {
                                    Utility.InvokeMethod(mi, null, pars: pars, session: requestData.Session);
                                    context.Response.ContentType = "text/json";
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("");
                                    return;
                                }
                                else if (mi.ReturnType==typeof(string))
                                {
                                    context.Response.ContentType = "text/text";
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync((string)Utility.InvokeMethod(mi, null, pars: pars, session: requestData.Session));
                                    return;
                                }
                                else
                                {
                                    context.Response.ContentType = "text/json";
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(mi, null, pars: pars, session:  requestData.Session)));
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Execution Error", ex);
                        }
                    }
                }
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                List<MethodInfo> methods = t.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0).ToList();
                if (methods.Count > 0)
                {
                    lock (_patterns)
                    {
                        _patterns.Add(new sMethodPatterns(Utility.GetModelUrlRoot(t), methods));
                    }
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            lock(_patterns)
            {
                foreach (Type t in types)
                    _patterns.RemoveAll(p => p.IsForType(t));
            }
        }
    }
}
