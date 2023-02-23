using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.ModelRequestHandlerBase;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class ModelActionHandler<T> : IModelActionHandler where T : IModel
    {
        private readonly IEnumerable<string> _baseURLs;
        public IEnumerable<string> BaseURLs => _baseURLs;

        private readonly MethodInfo _loadMethod;
        private readonly Dictionary<MethodInfo, IEnumerable<ASecurityCheck>> _securityChecks;
        private readonly IEnumerable<MethodInfo> _methods;

        public IEnumerable<string> MethodNames => _methods.Select(m => m.Name).Distinct();

        private readonly delRegisterSlowMethodInstance _registerSlowMethod;

        protected readonly string _callType;

        public ModelActionHandler(string callType, delRegisterSlowMethodInstance delRegisterSlowMethod)
            : this(new MethodInfo[] {}, callType, delRegisterSlowMethod)
        { }

        public ModelActionHandler(MethodInfo method, string callType,delRegisterSlowMethodInstance delRegisterSlowMethod)
            : this(new MethodInfo[] {method }, callType, delRegisterSlowMethod)
        {}

        public ModelActionHandler(IEnumerable<MethodInfo> methods,string callType, delRegisterSlowMethodInstance delRegisterSlowMethod)
        {
            _methods= methods;
            _callType=callType;
            _registerSlowMethod=delRegisterSlowMethod;
            _baseURLs = typeof(T)
               .GetCustomAttributes(typeof(ModelRoute), false)
               .Select(ca => ((ModelRoute)ca).Path)
               .OrderByDescending(p => p.Length);
            var modelChecks = typeof(T).GetCustomAttributes().OfType<ASecurityCheck>();
            _securityChecks = new Dictionary<MethodInfo, IEnumerable<ASecurityCheck>>();
            _loadMethod = typeof(T).GetMethods(Constants.LOAD_METHOD_FLAGS).FirstOrDefault(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0);
            if (_loadMethod!=null)
                _securityChecks.Add(_loadMethod, modelChecks.Concat(_loadMethod.GetCustomAttributes().OfType<ASecurityCheck>()));
            foreach (MethodInfo mi in _methods)
                _securityChecks.Add(mi, modelChecks.Concat(mi.GetCustomAttributes().OfType<ASecurityCheck>()));
        }

        public IModel Load(string url, ModelRequestData request, Func<string, string> extractID = null)
        {
            if (_loadMethod != null)
            {
                var id = extractID == null ? url.Substring(url.LastIndexOf("/") + 1) : extractID(url);
                if (_securityChecks[_loadMethod].Any(asc => !asc.HasValidAccess(request, null, url, id)))
                    throw new InsecureAccessException();
                Logger.Trace("Attempting to load model at url {0}", new object[] { url });
                var result = Utility.InvokeLoad(_loadMethod, id, request.Session);
                if (result != null)
                    return result;
            }
            throw new CallNotFoundException("Model Not Found");
        }

        public async Task Invoke(string url, ModelRequestData request, HttpContext context, Func<string, string> extractID = null, Func<IModel, ModelRequestData, IModel> processLoadedModel = null)
        {
            await _Invoke(url,request, context, Load(url, request, extractID: extractID),processLoadedModel: processLoadedModel);
        }

        public async Task InvokeWithoutLoad(string url, ModelRequestData request, HttpContext context, IModel model=null, Func<IModel, object, object[], MethodInfo, object> extractResponse = null)
        {
            await _Invoke(url, request, context, model, extractResponse: extractResponse);
        }

        private async Task _Invoke(string url, ModelRequestData request, HttpContext context,IModel model,Func<IModel, ModelRequestData, IModel> processLoadedModel = null, Func<IModel, object, object[], MethodInfo, object> extractResponse = null)
        {
            Logger.Trace("calling {0} method matching the url {1}", new object[] { _callType, url });
            MethodInfo method = null;
            object[] pars = null;
            Utility.LocateMethod(request, _methods, out method, out pars);
            if (method==null)
            {
                method = _methods.First();
                if (!method.GetCustomAttributes().Any(att=>att is ModelUpdateMethod || att is ModelSaveMethod))
                    throw new CallNotFoundException("Unable to locate method with matching parameters");
            }
            if (_securityChecks[method].Any(asc => !asc.HasValidAccess(request, model, url, (model==null ? null : model.id))))
                throw new InsecureAccessException();
            if (processLoadedModel != null)
                model = (T)processLoadedModel(model, request);
            Logger.Trace("Invoking the {0} method {1}.{2} for the url {3}", new object[] { _callType, typeof(T).FullName, method.Name, url });
            if (method.GetCustomAttributes().OfType<ExposedMethod>().Any(em=>em.IsSlow))
            {
                string newPath = _registerSlowMethod(url, method, model, pars, request.Session);
                if (newPath!= null)
                {
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(Utility.JsonEncode(newPath));
                }
                else
                    throw new SlowMethodRegistrationFailed();
            }
            else
            {
                if (method.ReturnType == typeof(void))
                {
                    Utility.InvokeMethod(method, model, pars: pars, session: request.Session);
                    context.Response.ContentType= "text/json";
                    context.Response.StatusCode= 200;
                    await context.Response.WriteAsync("");
                }
                else if (method.ReturnType==typeof(string))
                {
                    context.Response.StatusCode= 200;
                    string tmp = (string)Utility.InvokeMethod(method, model, pars: pars, session: request.Session);
                    context.Response.ContentType= (tmp==null ? "text/json" : "text/text");
                    await context.Response.WriteAsync((tmp==null ? Utility.JsonEncode(tmp) : tmp));
                }
                else
                {
                    context.Response.ContentType= "text/json";
                    context.Response.StatusCode= 200;
                    var resp = Utility.InvokeMethod(method, model, pars: pars, session: request.Session);
                    if (extractResponse!=null)
                        resp = extractResponse(model, resp,pars,method);
                    await context.Response.WriteAsync(Utility.JsonEncode(resp));
                }
            }
        }
    }
}
