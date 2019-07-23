using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class LoadHandler : IRequestHandler
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadHandler()
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _methods.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            MethodInfo mi = null;
            lock (_methods)
            {
                if (_methods.ContainsKey(url.Substring(0, url.LastIndexOf("/"))))
                    mi = _methods[url.Substring(0, url.LastIndexOf("/"))];
            }
            if (mi != null)
            {
                if (!securityCheck.Invoke(mi.DeclaringType, mi, session))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode= 200;
                return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(null, new object[] { url.Substring(url.LastIndexOf("/")+1) })));
            }
            throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method == RequestHandler.RequestMethods.GET)
                return _methods.ContainsKey(url.Substring(0,url.LastIndexOf("/")));
            return false;
        }

        public void Init(List<Type> types)
        {
            _methods.Clear();
            lock (_methods)
            {
                foreach (Type t in types)
                {
                    foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        {
                            _methods.Add(Utility.GetModelUrlRoot(t), mi);
                            break;
                        }
                    }
                }
            }
        }
    }
}
