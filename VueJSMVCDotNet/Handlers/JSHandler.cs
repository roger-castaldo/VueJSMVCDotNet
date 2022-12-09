using Microsoft.AspNetCore.Http;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.JSGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers
{
    internal class JSHandler : IRequestHandler
    {
        public struct sModelType
        {
            private Type _type;
            public Type Type { get { return _type; } }

            private PropertyInfo[] _properties;
            public PropertyInfo[] Properties { 
                get { 
                    if (_properties==null)
                    {
                        List<PropertyInfo> props = new List<PropertyInfo>();
                        foreach (PropertyInfo pi in _type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id")
                            {
                                if (!pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0)
                                    props.Add(pi);
                            }
                        }
                        _properties=props.ToArray();
                    }
                    return _properties; 
                } 
            }

            private MethodInfo[] _instanceMethods;
            public MethodInfo[] InstanceMethods { 
                get { 
                    if (_instanceMethods==null)
                    {
                        List<MethodInfo> methods = new List<MethodInfo>();
                        foreach (MethodInfo mi in _type.GetMethods(Constants.INSTANCE_METHOD_FLAGS))
                        {
                            if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                                methods.Add(mi);
                        }
                        _instanceMethods = methods.ToArray();
                    }
                    return _instanceMethods; 
                } 
            }

            private MethodInfo[] _staticMethods;
            public MethodInfo[] StaticMethods { 
                get { 
                    if (_staticMethods==null){
                        List<MethodInfo> methods = new List<MethodInfo>();
                        foreach (MethodInfo mi in _type.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS))
                        {
                            if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                                methods.Add(mi);
                        }
                        _staticMethods = methods.ToArray();
                    }
                    return _staticMethods; 
                } 
            }

            private sModelType[] _linkedTypes;
            public sModelType[] LinkedTypes
            {
                get
                {
                    if (_linkedTypes==null)
                    {
                        List<sModelType> types = new List<sModelType>();
                        foreach (PropertyInfo pi in Properties)
                        {
                            if (pi.CanRead)
                            {
                                Type t = pi.PropertyType;
                                if (t.IsArray)
                                    t = t.GetElementType();
                                else if (t.IsGenericType)
                                    t = t.GetGenericArguments()[0];
                                if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                                {
                                    if (!types.Contains(new sModelType(t)))
                                    {
                                        types.Add(new sModelType(t));
                                    }
                                }
                            }
                        }
                        foreach (MethodInfo[] methods in new MethodInfo[][] { InstanceMethods, StaticMethods })
                        {
                            foreach (MethodInfo mi in methods)
                            {
                                Type t = mi.ReturnType;
                                if (t.IsArray)
                                    t = t.GetElementType();
                                else if (t.IsGenericType)
                                    t = t.GetGenericArguments()[0];
                                if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                                {
                                    if (!types.Contains(new sModelType(t)))
                                    {
                                        types.Add(new sModelType(t));
                                    }
                                }
                                t = ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).ArrayElementType;
                                if (t!=null)
                                {
                                    if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                                    {
                                        if (!types.Contains(new sModelType(t)))
                                        {
                                            types.Add(new sModelType(t));
                                        }
                                    }
                                }
                            }
                        }
                        _linkedTypes=types.ToArray();
                    }
                    return _linkedTypes;
                }
            }

            private MethodInfo _saveMethod;
            public bool HasSave { get { return _saveMethod!=null; } }
            public MethodInfo SaveMethod { get { return _saveMethod; } }
            private MethodInfo _updateMethod;
            public bool HasUpdate { get { return _updateMethod!=null; } }
            public MethodInfo UpdateMethod { get { return _updateMethod; } }
            private MethodInfo _deleteMethod;
            public bool HasDelete { get { return _deleteMethod!=null; } }
            public MethodInfo DeleteMethod { get { return _deleteMethod; } }

            public sModelType(Type type)
            {
                _type = type;
                _properties=null;
                _instanceMethods=null;
                _staticMethods=null;
                _linkedTypes=null;
                _saveMethod=null;
                _updateMethod=null;
                _deleteMethod=null;
                foreach (MethodInfo mi in _type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                {
                    if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                        _saveMethod=mi;
                    else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                        _updateMethod=mi;
                    else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                        _deleteMethod=mi;
                }
            }

            public override bool Equals(object obj)
            {
                return (obj is sModelType && _type.FullName==((sModelType)obj).Type.FullName)
                    || (obj is Type && _type.FullName==((Type)obj).FullName);
            }
        }

        private static readonly IBasicJSGenerator[] _oneTimeInitialGenerators = new IBasicJSGenerator[]{
            new HeaderGenerator(),
            new TypingHeader(),
            new EventClassGenerator(),
            new ListClassGenerator(),
            new DefaultMethodsGenerator(),
            new ParsersGenerator()
        };

        private static readonly IBasicJSGenerator[] _oneTimeFinishGenerators = new IBasicJSGenerator[]{
            new FooterGenerator()
        };

        private static readonly IJSGenerator[] _classGenerators = new IJSGenerator[]
        {
            new ModelClassHeaderGenerator(),
            new JSONGenerator(),
            new ModelDefaultMethodsGenerator(),
            new ParseGenerator(),
            new ModelInstanceFooterGenerator(),
            new ModelLoadAllGenerator(),
            new ModelLoadGenerator(),
            new MethodsGenerator(),
            new ModelListCallGenerator(),
            new ModelClassFooterGenerator()
        };

        private Dictionary<string, string> _cache;
        private Dictionary<Type,ModelJSFilePath[]> _types;
        private readonly string _urlBase;
        private readonly string _vueImportPath;
        public JSHandler(string urlBase,string vueImportPath)
        {
            _cache = new Dictionary<string, string>();
            _types = new Dictionary<Type, ModelJSFilePath[]>();
            _urlBase=urlBase;
            _vueImportPath=vueImportPath;
        }

        public void ClearCache()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }

        public bool HandlesRequest(string url, ModelRequestHandler.RequestMethods method)
        {
            Logger.Trace("Checking if {0}:{1} is handled by the JS Handler", new object[] { method,url });
            if (method != ModelRequestHandler.RequestMethods.GET)
                return false;
            bool ret = false;
            lock (_cache)
            {
                if (_cache.ContainsKey(url))
                    ret = true;
            }
            if (!ret && _types != null)
            {
                lock(_types){
                    foreach (Type t in _types.Keys)
                    {
                        foreach (ModelJSFilePath mjsfp in _types[t])
                        {
                            if (mjsfp.IsMatch(url))
                            {
                                ret = true;
                                break;
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public Task HandleRequest(string url, ModelRequestHandler.RequestMethods method, Hashtable formData, HttpContext context, ISecureSession session, IsValidCall securityCheck)
        {
            Logger.Trace("Attempting to handle {0}:{1} with the JS Handler", new object[] { method, url });
            if (!HandlesRequest(url, method))
                throw new CallNotFoundException();
            else
            {
                List<Type> models = new List<Type>();
                if (_types != null)
                {
                    lock(_types){
                        foreach (Type t in _types.Keys)
                        {
                            foreach (ModelJSFilePath mjsfp in _types[t])
                            {
                                if (mjsfp.IsMatch(url))
                                {
                                    models.Add(t);
                                    break;
                                }
                            }
                        }
                    }
                    foreach (Type model in models){
                        if (!securityCheck.Invoke(model, null, session,null,url,null))
                            throw new InsecureAccessException();
                    }
                }
                DateTime modDate = DateTime.MinValue;
                foreach (Type model in models)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(model.Assembly.Location);
                        if (fi.Exists)
                            modDate = new DateTime(Math.Max(modDate.Ticks, fi.LastWriteTime.Ticks));
                    }
                    catch (Exception e) {
                        Logger.LogError(e);
                    }
                }
                if (modDate == DateTime.MinValue)
                    modDate = ModelRequestHandler.StartTime;
                if (context.Request.Headers.ContainsKey("If-Modified-Since"))
                {
                    DateTime lastModified = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
                    if (modDate.ToString()==lastModified.ToString()) { 
                        context.Response.StatusCode = 304;
                        return Task.CompletedTask;
                    }
                }
                string ret = null;
                Logger.Trace("Checking cache for existing js file for {0}", new object[] { url });
                context.Response.ContentType= "text/javascript";
                context.Response.StatusCode= 200;
                lock (_cache)
                {
                    if (_cache.ContainsKey(url))
                        ret = _cache[url];
                }
                if (ret == null && models.Count>0)
                {
                    sModelType[] amodels = new sModelType[models.Count];
                    for (int x = 0; x<models.Count; x++)
                        amodels[x] = new sModelType(models[x]);
                    Logger.Trace("No cached js file for {0}, generating new...", new object[] { url });
                    WrappedStringBuilder builder = new WrappedStringBuilder(url.ToLower().EndsWith(".min.js"));
                    builder.AppendLine(string.Format(@"import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from ""{0}"";
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.0'; }}", _vueImportPath));
                    foreach (IBasicJSGenerator gen in _oneTimeInitialGenerators)
                    {
                        builder.AppendLine(string.Format("//START:{0}", gen.GetType().Name));
                        gen.GeneratorJS(ref builder, _urlBase,amodels);
                        builder.AppendLine(string.Format("//END:{0}", gen.GetType().Name));
                    }
                    foreach (sModelType model in amodels) {
                        Logger.Trace("Processing module {0} for js url {1}", new object[] { model.Type.FullName, url });
                        foreach (IJSGenerator gen in _classGenerators)
                        {
                            builder.AppendLine(string.Format("//START:{0}", gen.GetType().Name));
                            gen.GeneratorJS(ref builder, model, _urlBase);
                            builder.AppendLine(string.Format("//END:{0}", gen.GetType().Name));
                        }
                    }
                    foreach (IBasicJSGenerator gen in _oneTimeFinishGenerators)
                    {
                        builder.AppendLine(string.Format("//START:{0}", gen.GetType().Name));
                        gen.GeneratorJS(ref builder, _urlBase, amodels);
                        builder.AppendLine(string.Format("//END:{0}", gen.GetType().Name));
                    }
                    ret = builder.ToString();
                    lock (_cache)
                    {
                        if (!_cache.ContainsKey(url))
                        {
                            Logger.Trace("Caching generated js file for {0}", new object[] { url });
                            _cache.Add(url, ret);
                        }
                    }
                }
                context.Response.Headers.Add("Last-Modified", modDate.ToUniversalTime().ToString("R"));
                context.Response.Headers.Add("Cache-Control", "public");
                return context.Response.WriteAsync(ret);
            }
        }

        public void Init(List<Type> types)
        {
            lock (_types)
            {
                foreach (Type t in types)
                {
                    ModelJSFilePath[] paths = (ModelJSFilePath[])t.GetCustomAttributes(typeof(ModelJSFilePath), false);
                    if (paths != null && paths.Length > 0)
                        _types.Add(t, paths);
                }
            }
        }

        #if NET

        public void LoadTypes(List<Type> types){
            lock (_types)
            {
                foreach (Type t in types)
                {
                    if (!_types.ContainsKey(t))
                    {
                        ModelJSFilePath[] paths = (ModelJSFilePath[])t.GetCustomAttributes(typeof(ModelJSFilePath), false);
                        if (paths != null && paths.Length > 0)
                            _types.Add(t, paths);
                    }
                }
            }
        }
        public void UnloadTypes(List<Type> types)
        {
            lock (_cache)
            {
                _cache.Clear();
            }
            lock (_types)
            {
                foreach (Type t in types)
                {
                    _types.Remove(t);
                }
            }
        }
        #endif
    }
}
