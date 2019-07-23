using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class SaveHandler : IRequestHandler
    {
        private Dictionary<string, ConstructorInfo> _constructors;
        private Dictionary<string, MethodInfo> _saveMethods;

        public SaveHandler()
        {
            _constructors = new Dictionary<string, ConstructorInfo>();
            _saveMethods = new Dictionary<string, MethodInfo>();
        }

        public void ClearCache()
        {
            _constructors.Clear();
            _saveMethods.Clear();
        }

        public Task HandleRequest(string url, RequestHandler.RequestMethods method, string formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            IModel model = null;
            lock (_constructors)
            {
                if (_constructors.ContainsKey(url))
                    model = (IModel)_constructors[url].Invoke(new object[] { });
            }
            if (model == null)
                throw new CallNotFoundException("Model Not Found");
            else
            {
                MethodInfo mi = null;
                lock (_saveMethods)
                {
                    if (_saveMethods.ContainsKey(url))
                        mi = _saveMethods[url];
                }
                if (mi != null)
                {
                    if (!securityCheck.Invoke(mi.DeclaringType, mi, session))
                        throw new InsecureAccessException();
                    Utility.SetModelValues(formData, ref model, false);
                    if ((bool)mi.Invoke(model, new object[] { }))
                    {
                        context.Response.ContentType = "text/json";
                        context.Response.StatusCode= 200;
                        return context.Response.WriteAsync(JSON.JsonEncode(new Hashtable() { { "id", model.id } }));
                    }
                    throw new Exception("Failed");
                }
                throw new CallNotFoundException();
            }
        }

        public bool HandlesRequest(string url, RequestHandler.RequestMethods method)
        {
            if (method == RequestHandler.RequestMethods.PUT)
                return _saveMethods.ContainsKey(url)&& _constructors.ContainsKey(url);
            return false;
        }

        public void Init(List<Type> types)
        {
            lock (_saveMethods)
            {
                _saveMethods.Clear();
                _constructors.Clear();
                foreach (Type t in types)
                {
                    foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                        {
                            _saveMethods.Add(Utility.GetModelUrlRoot(t), mi);
                            _constructors.Add(Utility.GetModelUrlRoot(t), t.GetConstructor(Type.EmptyTypes));
                            break;
                        }
                    }
                }
            }
        }
    }
}
