using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

        public string HandleRequest(string url, RequestHandler.RequestMethods method, string formData, out string contentType, out int responseStatus)
        {
            MethodInfo mi = null;
            lock (_methods)
            {
                if (_methods.ContainsKey(url))
                    mi = _methods[url];
            }
            if (mi != null)
            {
                contentType = "text/json";
                responseStatus = 200;
                return JSON.JsonEncode(mi.Invoke(null, new object[] { }));
            }
            else
            {
                contentType = "text/text";
                responseStatus = 404;
                return "Not Found";
            }
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
