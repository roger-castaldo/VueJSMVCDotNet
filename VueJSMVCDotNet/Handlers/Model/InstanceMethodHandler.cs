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
    internal class InstanceMethodHandler : ModelRequestHandlerBase
    {
        private struct sMethodPatterns
        {
            private Regex _reg;
            private MethodInfo _loadMethod;
            public MethodInfo LoadMethod => _loadMethod;
            private Dictionary<string, List<MethodInfo>> _methods;
            public Dictionary<string, List<MethodInfo>> Methods => _methods;
            private List<MethodInfo> _slowMethods;
            public List<MethodInfo> SlowMethods=> _slowMethods;

            public sMethodPatterns(string baseURL,MethodInfo loadMethod,List<MethodInfo> functions)
            {
                _loadMethod = loadMethod;
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
                _reg = new Regex(string.Format("^{0}/([^/]+)/({1})$", new object[] { baseURL, sb.ToString() }), RegexOptions.Compiled | RegexOptions.ECMAScript);
            }

            public bool IsForType(Type type){
                return _loadMethod.DeclaringType == type;
            }

            public bool IsValid(string url)
            {
                return _reg.IsMatch(url);
            }

            internal void ExtractIdAndMethod(string url, out string id, out string smethod)
            {
                Match m = _reg.Match(url);
                id = m.Groups[1].Value;
                smethod = m.Groups[2].Value;
            }
        }

        private List<sMethodPatterns> _patterns;

        public InstanceMethodHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory, registerSlowMethod, urlBase)
        {
            _patterns = new List<sMethodPatterns>();
        }

        public override void ClearCache()
        {
            _patterns.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            sMethodPatterns? methodPattern = null;
            string url = _CleanURL(context);
            if (GetRequestMethod(context)==ModelRequestHandler.RequestMethods.METHOD)
            {
                lock (_patterns)
                {
                    methodPattern = _patterns.Where(p => p.IsValid(url)).FirstOrDefault();
                }
            }
            if (methodPattern!=null)
            {
                Logger.Trace("Attempting to handle request {0} with an Instance Method", new object[] { url });
                sRequestData requestData = await _ExtractParts(context);
                if (!await _ValidCall(methodPattern.Value.LoadMethod.DeclaringType, methodPattern.Value.LoadMethod, null,context))
                    throw new InsecureAccessException();
                string id;
                string smethod;
                methodPattern.Value.ExtractIdAndMethod(url, out id, out smethod);
                Logger.Trace("Attempting to load model at {0} for invoked an instance method", new object[] { url });
                IModel model = Utility.InvokeLoad(methodPattern.Value.LoadMethod, id, requestData.Session);
                if (model == null)
                    throw new CallNotFoundException("Model Not Found");
                MethodInfo mi;
                object[] pars;
                Logger.Trace("Attempting to locate the appropriate version of the instance method at {0} with the supplied parameters", new object[] { url });
                Utility.LocateMethod(requestData.FormData, methodPattern.Value.Methods[smethod], out mi, out pars,requestData.Session);
                if (mi == null)
                    throw new CallNotFoundException("Unable to locate requested method to invoke");
                else
                {
                    if (!await _ValidCall(methodPattern.Value.LoadMethod.DeclaringType, mi, model, context))
                        throw new InsecureAccessException();
                    try
                    {
                        Logger.Trace("Using {0}.{1} to invoke the Instance Method at the url {2}", new object[] { mi.DeclaringType.FullName, mi.Name, url });
                        if (methodPattern.Value.SlowMethods.Contains(mi))
                        {
                            string newPath = _RegisterSlowMethodInstance(url, mi, model, pars,requestData.Session);
                            if (newPath!= null)
                            {
                                context.Response.ContentType = "text/json";
                                context.Response.StatusCode = 200;
                                await context.Response.WriteAsync(JSON.JsonEncode(newPath));
                            }
                            else
                                throw new SlowMethodRegistrationFailed();
                        }
                        else
                        {
                            if (mi.ReturnType == typeof(void))
                            {
                                Utility.InvokeMethod(mi, model, pars: pars, session: requestData.Session);
                                context.Response.ContentType= "text/json";
                                context.Response.StatusCode= 200;
                                await context.Response.WriteAsync("");
                            }
                            else if (mi.ReturnType==typeof(string))
                            {
                                context.Response.StatusCode= 200;
                                string tmp = (string)Utility.InvokeMethod(mi, model, pars: pars, session: requestData.Session);
                                context.Response.ContentType= (tmp==null ? "text/json" : "text/text");
                                await context.Response.WriteAsync((tmp==null ? JSON.JsonEncode(tmp) : tmp));
                            }
                            else
                            {
                                context.Response.ContentType= "text/json";
                                context.Response.StatusCode= 200;
                                await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(mi, model, pars: pars, session: requestData.Session)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Execution Error", ex);
                    }
                }
            }
            else
                await _next(context);
        }
        protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                MethodInfo loadMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m=>m.GetCustomAttributes(typeof(ModelLoadMethod),false).Length>0).FirstOrDefault();
                if (loadMethod != null)
                {
                    var methods = t.GetMethods(Constants.INSTANCE_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ExposedMethod), false).Length>0).ToList();
                    if (methods.Count>0)
                        _patterns.Add(new sMethodPatterns(Utility.GetModelUrlRoot(t), loadMethod, methods));
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                lock (_patterns)
                {
                    _patterns.RemoveAll(p => p.IsForType(t));
                }
            }
        }

    }
}
