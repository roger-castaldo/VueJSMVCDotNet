using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;
using static System.Collections.Specialized.BitVector32;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class UpdateHandler : ModelRequestHandlerBase
    {
        private Dictionary<string, MethodInfo> _loadMethods;
        private Dictionary<string, MethodInfo> _updateMethods;

        public UpdateHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            : base(next,sessionFactory, registerSlowMethod, urlBase)
        {
            _loadMethods = new Dictionary<string, MethodInfo>();
            _updateMethods = new Dictionary<string, MethodInfo>();
        }

        public override void ClearCache()
        {
            _loadMethods.Clear();
            _updateMethods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            Logger.Trace("Checking if the request {0}:{1} is handled by the Update Handler", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)== ModelRequestHandler.RequestMethods.PATCH)
            {
                MethodInfo loadMethod = null;
                MethodInfo updateMethod = null;
                lock (_loadMethods)
                {
                    loadMethod = _loadMethods.ContainsKey(url.Substring(0,url.LastIndexOf("/"))) ? _loadMethods[url.Substring(0, url.LastIndexOf("/"))] : null;
                }
                lock (_updateMethods){
                    updateMethod = _updateMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))) ? _updateMethods[url.Substring(0, url.LastIndexOf("/"))] : null;
                }
                if (loadMethod!= null && updateMethod!=null)
                {
                    Logger.Trace("Attempting to handle {0}:{1} request in the Update Handler", new object[] { GetRequestMethod(context), url });
                    sRequestData requestData = await _ExtractParts(context);
                    IModel model = null;
                    if (!await _ValidCall(loadMethod.DeclaringType, loadMethod, model, context))
                        throw new InsecureAccessException();
                    Logger.Trace("Attempting to load model at url {0}", new object[] { url });
                    model = Utility.InvokeLoad(_loadMethods[url.Substring(0, url.LastIndexOf("/"))], url.Substring(url.LastIndexOf("/") + 1), requestData.Session);
                    if (model == null)
                        throw new CallNotFoundException("Model Not Found");
                    else
                    {
                        MethodInfo mi = null;
                        lock (_updateMethods)
                        {
                            if (_updateMethods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                                mi = _updateMethods[url.Substring(0, url.LastIndexOf("/"))];
                        }
                        if (mi != null)
                        {
                            if (!await _ValidCall(updateMethod.DeclaringType, updateMethod, model, context))
                                throw new InsecureAccessException();
                            context.Response.ContentType = "text/json";
                            context.Response.StatusCode= 200;
                            Logger.Trace("Attempting to handle an update request with {0}.{1} in the model with id {2}", new object[] { model.GetType().FullName, mi.Name, model.id });
                            Utility.SetModelValues(requestData.FormData, ref model, false);
                            await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(mi, model, session: requestData.Session)));
                            return;
                        }
                    }
                }
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types){
            foreach (Type t in types)
            {
                MethodInfo updateMethod = t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0).FirstOrDefault();
                if (updateMethod != null)
                {
                    _updateMethods.Add(Utility.GetModelUrlRoot(t), updateMethod);
                    _loadMethods.Add(Utility.GetModelUrlRoot(t), t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0).First());
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            string[] keys;
            lock (_updateMethods)
            {
                keys = new string[_updateMethods.Count];
                _updateMethods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_updateMethods[str].DeclaringType))
                    {
                        _updateMethods.Remove(str);
                    }
                }
            }
            lock (_loadMethods)
            {
                keys = new string[_loadMethods.Count];
                _loadMethods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_loadMethods[str].DeclaringType))
                    {
                        _loadMethods.Remove(str);
                    }
                }
            }
        }
    }
}
