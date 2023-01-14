using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal class LoadAllHandler : ModelRequestHandlerBase
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadAllHandler(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, string urlBase)
            : base(next, sessionFactory, registerSlowMethod, urlBase)
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public override void ClearCache()
        {
            _methods.Clear();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            bool found = false;
            if (context.Request.Method.ToUpper()=="GET")
            {
                string url = _CleanURL(context);
                MethodInfo mi = null;
                lock (_methods)
                {
                    if (_methods.ContainsKey(url))
                        mi = _methods[url];
                }
                if (mi!=null)
                {
                    found=true;
                    var reqData = await _ExtractParts(context);
                    if (!await _ValidCall(mi.DeclaringType, mi,null,context))
                        throw new InsecureAccessException();
                    context.Response.ContentType = "text/json";
                    context.Response.StatusCode = 200;
                    Logger.Trace("Invoking the Load All call {0}.{1} to handle {2}:{3}", new object[] { mi.DeclaringType.FullName, mi.Name, context.Request.Method.ToUpper(), url });
                    await context.Response.WriteAsync(JSON.JsonEncode(Utility.InvokeMethod(mi, null, session: reqData.Session)));
                }
            }
            if (!found)
                await _next(context);
        }

        protected override void _LoadTypes(List<Type> types)
        {
            lock (_methods)
            {
                foreach (Type t in types)
                {
                    MethodInfo loadAllMethod = t.GetMethods(Constants.LOAD_METHOD_FLAGS).Where(m => m.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length>0).FirstOrDefault();
                    if (loadAllMethod!=null)
                        _methods.Add(Utility.GetModelUrlRoot(t), loadAllMethod);
                }
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
                        _methods.Remove(str);
                }
            }
        }
    }
}
