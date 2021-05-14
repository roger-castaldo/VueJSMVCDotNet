using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class LoadAllHandler : IRequestHandler
    {
        private Dictionary<string, MethodInfo> _methods;

        public LoadAllHandler()
        {
            _methods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _methods.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            MethodInfo mi = null;
            lock (_methods)
            {
                if (_methods.ContainsKey(url))
                    mi = _methods[url];
            }
            if (mi != null)
            {
                if (!securityCheck.Invoke(mi.DeclaringType, mi, session,null,url,null))
                    throw new InsecureAccessException();
                context.Response.ContentType = "text/json";
                context.Response.StatusCode = 200;
                return context.Response.WriteAsync(JSON.JsonEncode(mi.Invoke(null, (mi.GetParameters().Length==1 ? new object[]{session} : new object[] { }))));
            }
            else
                throw new CallNotFoundException();
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method==RequestHandler.RequestMethods.GET)
                return _methods.ContainsKey(url);
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
                        if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
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
