using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;
using Microsoft.Extensions.Caching.Memory;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class JSHandler : ModelRequestHandlerBase
    {

        public struct sModelType
        {
            private readonly Type _type;
            public Type Type { get { return _type; } }

            private readonly IEnumerable<PropertyInfo> _properties;
            public IEnumerable<PropertyInfo> Properties => _properties;

            private readonly IEnumerable<MethodInfo> _instanceMethods;
            public IEnumerable<MethodInfo> InstanceMethods => _instanceMethods; 

            private readonly IEnumerable<MethodInfo> _staticMethods;
            public IEnumerable<MethodInfo> StaticMethods => _staticMethods;

            private static Type _ExtractType(Type t)
            {
                if (t.IsArray)
                    t = t.GetElementType();
                else if (t.IsGenericType)
                    t = t.GetGenericArguments()[0];
                return t;
            }

            private IEnumerable<sModelType> _linkedTypes;
            public IEnumerable<sModelType> LinkedTypes
            {
                get
                {
                    if (_linkedTypes==null)
                    {
                        _linkedTypes = Properties.Where(pi => pi.CanRead)
                            .Select(pi => _ExtractType(pi.PropertyType))
                            .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                            .Select(t => new sModelType(t))
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi=>_ExtractType(mi.ReturnType))
                                .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                                .Select(t => new sModelType(t))
                            )
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi => ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).ArrayElementType)
                                .Where(t => t!=null && t.GetInterfaces().Contains(typeof(IModel)))
                                .Select(t => new sModelType(t))
                            )
                            .Distinct();
                    }
                    return _linkedTypes;
                }
            }

            private readonly MethodInfo _saveMethod;
            public bool HasSave { get { return _saveMethod!=null; } }
            public MethodInfo SaveMethod { get { return _saveMethod; } }
            private readonly MethodInfo _updateMethod;
            public bool HasUpdate { get { return _updateMethod!=null; } }
            public MethodInfo UpdateMethod { get { return _updateMethod; } }
            private readonly MethodInfo _deleteMethod;
            public bool HasDelete { get { return _deleteMethod!=null; } }
            public MethodInfo DeleteMethod { get { return _deleteMethod; } }

            public sModelType(Type type)
            {
                _type = type;
                _properties=_type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi=> pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id"
                                && !pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0);
                _instanceMethods=_type.GetMethods(Constants.INSTANCE_METHOD_FLAGS).Where(mi=> mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                _staticMethods=_type.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                _linkedTypes = null;
                _saveMethod = _type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0);
                _updateMethod=_type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0);
                _deleteMethod=_type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0);
            }

            public override bool Equals(object obj)
            {
                return (obj is sModelType && _type.FullName==((sModelType)obj).Type.FullName)
                    || (obj is Type && _type.FullName==((Type)obj).FullName);
            }

            public override int GetHashCode()
            {
                return _type.FullName.GetHashCode();
            }
        }

        private static readonly IBasicJSGenerator[] _oneTimeInitialGenerators = new IBasicJSGenerator[]{
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

        private readonly List<string> _keys;
        private readonly IMemoryCache _cache;
        private readonly Dictionary<Type,ModelJSFilePath[]> _types;
        private readonly Dictionary<Type, IEnumerable<ASecurityCheck>> _securityChecks;
        private readonly string _urlBase;
        private readonly string _vueImportPath;
        private readonly string _coreImportPath;
        private readonly bool _compressAllJS;
        public JSHandler(string urlBase,string vueImportPath, string coreImportPath, string[] securityHeaders,
            RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, bool compressAllJS,IMemoryCache cache, ILog log)
            : base(next, sessionFactory, registerSlowMethod, urlBase,log)
        {
            _keys = new List<string>();
            _cache = cache;
            _types = new Dictionary<Type, ModelJSFilePath[]>();
            _urlBase=urlBase;
            _vueImportPath=vueImportPath;
            _coreImportPath=coreImportPath;
            _securityChecks=new Dictionary<Type, IEnumerable<ASecurityCheck>>();
            _compressAllJS=compressAllJS;
        }

        public override void ClearCache()
        {
            lock (_keys)
            {
                _keys.ForEach(key => _cache.Remove(key));
                _keys.Clear();
            }
            lock (_securityChecks)
            {
                _securityChecks.Clear();
            }
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            bool found = false;
            if (context.Request.Method.ToUpper()=="GET")
            {
                string url = _CleanURL(context);
                List<Type> models = new List<Type>();
                if (_types!=null)
                {
                    lock (_types)
                    {
                        foreach (Type t in _types.Keys)
                        {
                            foreach (ModelJSFilePath mjsfp in _types[t])
                            {
                                if (mjsfp.IsMatch(url))
                                    models.Add(t);
                            }
                        }
                    }
                }
                if (models.Count>0)
                {
                    found=true;
                    var reqData = await _ExtractParts(context);
                    foreach (Type model in models)
                    {
                        IEnumerable<ASecurityCheck> checks = new ASecurityCheck[] { };
                        lock (_securityChecks)
                        {
                            if (!_securityChecks.ContainsKey(model))
                                _securityChecks.Add(model,model.GetCustomAttributes().OfType<ASecurityCheck>());
                            checks = _securityChecks[model];
                        }
                        if (checks.Any(check=>!check.HasValidAccess(reqData,null,url,null)))
                            throw new InsecureAccessException();
                    }
                    DateTime modDate = DateTime.MinValue;
                    foreach (Type model in models)
                    {
                        FileInfo fi = new FileInfo(model.Assembly.Location);
                        if (fi.Exists)
                            modDate = new DateTime(Math.Max(modDate.Ticks, fi.LastWriteTime.Ticks));
                    }
                    if (modDate == DateTime.MinValue)
                        modDate = ModelRequestHandler.StartTime;
                    if (context.Request.Headers.ContainsKey("If-Modified-Since"))
                    {
                        DateTime lastModified = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
                        if (modDate.ToString()==lastModified.ToString())
                        {
                            context.Response.StatusCode = 304;
                            return;
                        }
                    }

                    string ret = null;
                    log.Trace("Checking cache for existing js file for {0}", new object[] { url });
                    context.Response.ContentType= "text/javascript";
                    context.Response.StatusCode= 200;
                    lock (_keys)
                    {
                        if (_keys.Contains(url))
                        {
                            try
                            {
                                ret = _cache.Get<string>(url);
                            }
                            catch (Exception)
                            {
                                _keys.Remove(url);
                            }
                            if (ret==null)
                                _keys.Remove(url);
                        }
                    }
                    if (ret == null && models.Count>0)
                    {
                        ret = _GenerateCode(models,url);
                        lock (_keys)
                        {
                            if (!_keys.Contains(url))
                            {
                                log.Trace("Caching generated js file for {0}", new object[] { url });
                                _keys.Add(url);
                                _cache.Set<string>(url, ret, RequestHandlerBase.CACHE_ENTRY_OPTIONS);
                            }
                        }
                    }
                    context.Response.Headers.Add("Last-Modified", modDate.ToUniversalTime().ToString("R"));
                    context.Response.Headers.Add("Cache-Control", "public");
                    await context.Response.WriteAsync(ret);
                }
            }
            if (!found)
                await _next(context);
        }

        private string _GenerateCode(List<Type> models,string url)
        {
            sModelType[] amodels = new sModelType[models.Count];
            for (int x = 0; x<models.Count; x++)
                amodels[x] = new sModelType(models[x]);
            log.Trace("No cached js file for {0}, generating new...", new object[] { url });
            WrappedStringBuilder builder = new WrappedStringBuilder(_compressAllJS || url.ToLower().EndsWith(".min.js"));
            builder.AppendLine(@$"import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{_coreImportPath}';
import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from '{_vueImportPath}';
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.0'; }}");
            foreach (IBasicJSGenerator gen in _oneTimeInitialGenerators)
            {
                builder.AppendLine($"//START:{gen.GetType().Name}");
                gen.GeneratorJS(ref builder, _urlBase, amodels, log);
                builder.AppendLine($"//END:{gen.GetType().Name}");
            }
            foreach (sModelType model in amodels)
            {
                log.Trace("Processing module {0} for js url {1}", new object[] { model.Type.FullName, url });
                foreach (IJSGenerator gen in _classGenerators)
                {
                    builder.AppendLine($"//START:{gen.GetType().Name}");
                    gen.GeneratorJS(ref builder, model, _urlBase, log);
                    builder.AppendLine($"//END:{gen.GetType().Name}");
                }
            }
            foreach (IBasicJSGenerator gen in _oneTimeFinishGenerators)
            {
                builder.AppendLine($"//START:{gen.GetType().Name}");
                gen.GeneratorJS(ref builder, _urlBase, amodels,log);
                builder.AppendLine($"//END:{gen.GetType().Name}");
            }
            return builder.ToString();
        }

        protected override void _LoadTypes(List<Type> types)
        {
            lock (_types)
            {
                foreach (Type t in types)
                {
                    ModelJSFilePath[] paths = (ModelJSFilePath[])t.GetCustomAttributes(typeof(ModelJSFilePath), false);
                    if (paths != null && paths.Length > 0)
                    {
                        if (_types.ContainsKey(t))
                            _types.Remove(t);
                        _types.Add(t, paths);
                    }
                }
            }
        }

        protected override void _UnloadTypes(List<Type> types)
        {
            ClearCache();
            lock (_types)
            {
                foreach (Type t in types)
                {
                    _types.Remove(t);
                }
            }
        }
    }
}
