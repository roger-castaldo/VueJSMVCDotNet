using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
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
        struct sUpdateMethodPair
        {
            private readonly MethodInfo _loadMethod;
            public MethodInfo LoadMethod => _loadMethod;
            private readonly MethodInfo _updateMethod;
            public MethodInfo UpdateMethod => _updateMethod;

            public Type DeclaringType => _loadMethod.DeclaringType;

            public sUpdateMethodPair(MethodInfo loadMethod, MethodInfo updateMethod)
            {
                _loadMethod= loadMethod;
                _updateMethod= updateMethod;
            }
        }

        private ConcurrentDictionary<string, sUpdateMethodPair> _methods;

        public UpdateHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            : base(next,sessionFactory, registerSlowMethod, urlBase)
        {
            _methods=new ConcurrentDictionary<string, sUpdateMethodPair>();
        }

        public override void ClearCache()
        {
            _methods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            Logger.Trace("Checking if the request {0}:{1} is handled by the Update Handler", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context)== ModelRequestHandler.RequestMethods.PATCH && _methods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
            {
                MethodInfo loadMethod = null;
                MethodInfo updateMethod = null;
                sUpdateMethodPair pair;
                if (_methods.TryGetValue(url.Substring(0, url.LastIndexOf("/")), out pair))
                {
                    loadMethod=pair.LoadMethod;
                    updateMethod= pair.UpdateMethod;
                }
                if (loadMethod!= null && updateMethod!=null)
                {
                    Logger.Trace("Attempting to handle {0}:{1} request in the Update Handler", new object[] { GetRequestMethod(context), url });
                    sRequestData requestData = await _ExtractParts(context);
                    IModel model = null;
                    if (!await _ValidCall(loadMethod.DeclaringType, loadMethod, model, context))
                        throw new InsecureAccessException();
                    Logger.Trace("Attempting to load model at url {0}", new object[] { url });
                    model = Utility.InvokeLoad(loadMethod, url.Substring(url.LastIndexOf("/") + 1), requestData.Session);
                    if (model == null)
                        throw new CallNotFoundException("Model Not Found");
                    else
                    {
                        if (!await _ValidCall(updateMethod.DeclaringType, updateMethod, model, context))
                                throw new InsecureAccessException();
                        context.Response.ContentType = "text/json";
                        context.Response.StatusCode= 200;
                        Logger.Trace("Attempting to handle an update request with {0}.{1} in the model with id {2}", new object[] { model.GetType().FullName, updateMethod.Name, model.id });
                        Utility.SetModelValues(requestData.FormData, ref model, false, requestData.Session);
                        await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(updateMethod, model, session: requestData.Session)));
                        return;
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
                    _methods.TryAdd(Utility.GetModelUrlRoot(t), new sUpdateMethodPair(
                        t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length>0).FirstOrDefault(),
                        updateMethod
                    ));
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            var keys = new string[_methods.Count];
            _methods.Keys.CopyTo(keys, 0);
            sUpdateMethodPair pair;
            foreach (string str in keys)
            {
                if (types.Contains(_methods[str].DeclaringType))
                    _methods.TryRemove(str, out pair);
            }
        }
    }
}
