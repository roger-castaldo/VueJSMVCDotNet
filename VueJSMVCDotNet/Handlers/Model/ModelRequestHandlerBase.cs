using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model
{
    internal abstract class ModelRequestHandlerBase
    {
        private const string _CONVERTED_URL_KEY = "PARSED_URL";
        private const string _REQUEST_DATA_KEY = "CONVERTED_REQUEST_DATA";

        public struct sRequestData
        {
            private readonly Hashtable _formData;
            public Hashtable FormData => _formData;
            private readonly ISecureSession _session;
            public ISecureSession Session => _session;

            public sRequestData(Hashtable formData,ISecureSession session)
            {
                _formData = formData;
                _session = session;
            }
        }

        protected readonly RequestDelegate _next;
        private readonly ISecureSessionFactory _sessionFactory;
        private readonly delRegisterSlowMethodInstance _registerSlowMethod;
        private readonly string _urlBase;
        private Dictionary<Type, ASecurityCheck[]> _typeChecks;
        private Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>> _methodChecks;

        public ModelRequestHandlerBase(RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod,string urlBase)
        {
            _next = next;
            _sessionFactory=sessionFactory;
            _registerSlowMethod=registerSlowMethod;
            _urlBase=urlBase;
            _typeChecks = new Dictionary<Type, ASecurityCheck[]>();
            _methodChecks = new Dictionary<Type, Dictionary<MethodInfo, ASecurityCheck[]>>();
        }

        protected async Task<sRequestData> _ExtractParts(HttpContext context)
        {
            if (!context.Items.ContainsKey(_REQUEST_DATA_KEY))
            {
                var session = _sessionFactory.ProduceFromContext(context);
                var formData = new Hashtable();
                if (context.Request.ContentType!=null &&
                (
                    context.Request.ContentType=="application/x-www-form-urlencoded"
                    || context.Request.ContentType.StartsWith("multipart/form-data")
                ))
                {
                    foreach (string key in context.Request.Form.Keys)
                    {
                        Logger.Trace("Loading form data value from key {0}", new object[] { key });
                        if (key.EndsWith(":json"))
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                ArrayList al = new ArrayList();
                                foreach (string str in context.Request.Form[key])
                                {
                                    al.Add(JSON.JsonDecode(str));
                                }
                                formData.Add(key.Substring(0, key.Length-5), al);
                            }
                            else
                            {
                                formData.Add(key.Substring(0, key.Length-5), JSON.JsonDecode(context.Request.Form[key][0]));
                            }
                        }
                        else
                        {
                            if (context.Request.Form[key].Count>1)
                            {
                                ArrayList al = new ArrayList();
                                foreach (string str in context.Request.Form[key])
                                {
                                    al.Add(str);
                                }
                                formData.Add(key, al);
                            }
                            else
                            {
                                formData.Add(key, context.Request.Form[key][0]);
                            }
                        }
                    }
                }
                else
                {
                    string tmp = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    if (tmp!="")
                    {
                        Logger.Trace("Loading form data from request body");
                        formData = (Hashtable)JSON.JsonDecode(tmp);
                    }
                }
                context.Items.Add(_REQUEST_DATA_KEY, new sRequestData(formData, session));
            }
            return (sRequestData)context.Items[_REQUEST_DATA_KEY];
        }

        protected string _CleanURL(HttpContext context)
        {
            if (!context.Items.ContainsKey(_CONVERTED_URL_KEY))
                context.Items.Add(_CONVERTED_URL_KEY,Utility.CleanURL(Utility.BuildURL(context, _urlBase)));
            return (string)context.Items[_CONVERTED_URL_KEY];
        }

        protected RequestMethods GetRequestMethod(HttpContext context)
        {
            return (RequestMethods)Enum.Parse(typeof(RequestMethods), context.Request.Method.ToUpper());
        }

        protected string _RegisterSlowMethodInstance(string url, MethodInfo method, object model, object[] pars, ISecureSession session)
        {
            return _registerSlowMethod.Invoke(url, method, model, pars, session);
        }

        protected async Task<bool> _ValidCall(Type t, MethodInfo method, IModel model,HttpContext context,string id=null)
        {
            sRequestData requestData = await _ExtractParts(context);
            string url = _CleanURL(context);
            if (id!= null)
                requestData.FormData.Add("id", id);
            Logger.Trace("Checking security for call {0} under class {1}.{2}", new object[] { url, t.FullName, (method==null ? null : method.Name) });
            List<ASecurityCheck> checks = new List<ASecurityCheck>();
            lock (_typeChecks)
            {
                if (_typeChecks.ContainsKey(t))
                    checks.AddRange(_typeChecks[t]);
                else
                {
                    List<ASecurityCheck> tchecks = new List<ASecurityCheck>();
                    foreach (object obj in t.GetCustomAttributes())
                    {
                        if (obj is ASecurityCheck)
                            checks.Add((ASecurityCheck)obj);
                    }
                    _typeChecks.Add(t, tchecks.ToArray());
                }
            }
            if (method != null)
            {
                lock (_methodChecks)
                {
                    bool add = true;
                    if (_methodChecks.ContainsKey(t))
                    {
                        if (_methodChecks[t].ContainsKey(method))
                        {
                            add=false;
                            checks.AddRange(_methodChecks[t][method]);
                        }
                    }
                    if (add)
                    {
                        var methodChecks = new Dictionary<MethodInfo, ASecurityCheck[]>();
                        if (_methodChecks.ContainsKey(t))
                        {
                            methodChecks= _methodChecks[t];
                            _methodChecks.Remove(t);
                        }
                        var mchecks = new List<ASecurityCheck>();
                        foreach (object obj in method.GetCustomAttributes())
                        {
                            if (obj is ASecurityCheck)
                                checks.Add((ASecurityCheck)obj);
                        }
                        methodChecks.Add(method, checks.ToArray());
                        _methodChecks.Add(t, methodChecks);
                    }
                }
            }
            foreach (ASecurityCheck asc in checks)
            {
                if (!asc.HasValidAccess(requestData.Session, model, url, requestData.FormData))
                    return false;
            }
            return true;
        }

        public void LoadTypes(List<Type> types)
        {
#if !NET
            _UnloadTypes(types);
#endif
            _LoadTypes(types);
        }
        public abstract void ClearCache();
        public abstract Task ProcessRequest(HttpContext context);
        protected abstract void _LoadTypes(List<Type> types);

#if NET
        public void UnloadTypes(List<Type> types)
        {
            lock (_typeChecks)
            {
                foreach (Type t in types)
                    _typeChecks.Remove(t);
            }
            lock (_methodChecks)
            {
                foreach (Type t in types)
                    _methodChecks.Remove(t);
            }
            _UnloadTypes(types);
        }
#endif
        protected abstract void _UnloadTypes(List<Type> types);
    }
}
