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
    internal class LoadHandler : ModelRequestHandlerBase
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            :base(next,sessionFactory,registerSlowMethod,urlBase)
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public override void ClearCache()
        {
            _methods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            string url = _CleanURL(context);
            Logger.Trace("Checking if the Load Handler handles {0}:{1}", new object[] { GetRequestMethod(context), url });
            if (GetRequestMethod(context) == ModelRequestHandler.RequestMethods.GET)
            {
                MethodInfo mi = null;
                lock (_methods)
                {
                    if (_methods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                        mi = _methods[url.Substring(0, url.LastIndexOf("/"))];
                }
                if (mi != null)
                {
                    if (! await _ValidCall(mi.DeclaringType,mi,null,context,id: url.Substring(url.LastIndexOf("/") + 1)))
                        throw new InsecureAccessException();
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode= 200;
                    string id = url.Substring(url.LastIndexOf("/")+1);
                    Logger.Trace("Attempting to load model using {0}.{1} with the id {2}", new object[] { mi.DeclaringType.FullName, mi.Name, id });
                    sRequestData requestData = await _ExtractParts(context);
                    await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeLoad(mi, id, requestData.Session)));
                    return;
                }
            }
            await _next(context);
        }

        protected override void _LoadTypes(List<Type> types)
        {
            foreach (Type t in types)
            {
                MethodInfo loadMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0).FirstOrDefault();
                if (loadMethod!=null)
                    _methods.Add(Utility.GetModelUrlRoot(t), loadMethod);
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            lock (_methods)
            {
                string[] keys = new string[_methods.Count];
                _methods.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (types.Contains(_methods[str].DeclaringType))
                    {
                        _methods.Remove(str);
                    }
                }
            }
        }
    }
}
