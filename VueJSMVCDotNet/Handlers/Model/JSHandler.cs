using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Threading;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.ModelRequestHandler;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class JSHandler : ModelRequestHandlerBase
    {

        public struct SModelType
        {
            public Type Type { get; private init; }
            public IEnumerable<PropertyInfo> Properties { get; private init; }
            public IEnumerable<MethodInfo> InstanceMethods { get; private init; }
            public IEnumerable<MethodInfo> StaticMethods { get; private init; }

            private static Type ExtractType(Type t)
            {
                if (t.IsArray)
                    t = t.GetElementType();
                else if (t.IsGenericType)
                    t = t.GetGenericArguments()[0];
                return t;
            }

            private IEnumerable<SModelType> _linkedTypes;
            public IEnumerable<SModelType> LinkedTypes
            {
                get
                {
                    return _linkedTypes ??= Properties.Where(pi => pi.CanRead)
                            .Select(pi => ExtractType(pi.PropertyType))
                            .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                            .Select(t => new SModelType(t))
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi=>ExtractType(mi.ReturnType))
                                .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                                .Select(t => new SModelType(t))
                            )
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi => ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).ArrayElementType)
                                .Where(t => t!=null && t.GetInterfaces().Contains(typeof(IModel)))
                                .Select(t => new SModelType(t))
                            )
                            .Distinct();
                }
            }
            public bool HasSave => SaveMethod!=null;
            public MethodInfo SaveMethod { get; private init; }
            public bool HasUpdate => UpdateMethod!=null;
            public MethodInfo UpdateMethod { get; private init; }
            public bool HasDelete => DeleteMethod!=null;
            public MethodInfo DeleteMethod { get; private init; }

            public SModelType(Type type)
            {
                Type = type;
                Properties=type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi=> pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id"
                                && !pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0);
                InstanceMethods=type.GetMethods(Constants.INSTANCE_METHOD_FLAGS).Where(mi=> mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                StaticMethods=type.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                _linkedTypes = null;
                SaveMethod = type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0);
                UpdateMethod=type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0);
                DeleteMethod=type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0);
            }

            public override bool Equals(object obj)
            {
                return (obj is SModelType model && Type.FullName==model.Type.FullName)
                    || (obj is Type type && Type.FullName==type.FullName);
            }

            public override int GetHashCode()
            {
                return Type.FullName.GetHashCode();
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
        private readonly ReaderWriterLockSlim _locker;
        private readonly Dictionary<Type,ModelJSFilePath[]> _types;
        private readonly Dictionary<Type, IEnumerable<ASecurityCheck>> _securityChecks;
        private readonly string _urlBase;
        private readonly string _vueImportPath;
        private readonly string _coreImportPath;
        private readonly bool _compressAllJS;
        public JSHandler(string urlBase,string vueImportPath, string coreImportPath,
            RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, bool compressAllJS,IMemoryCache cache, ILogger log)
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
            _locker = new ReaderWriterLockSlim();
        }

        public override void ClearCache()
        {
            _locker.EnterWriteLock();
            _keys.ForEach(key => _cache.Remove(key));
            _keys.Clear();
            _securityChecks.Clear();
            _locker.ExitWriteLock();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            bool found = false;
            if (context.Request.Method.ToUpper()=="GET")
            {
                string url = CleanURL(context);
                List<Type> models = new();
                _locker.EnterReadLock();
                if (_types!=null)
                    models = _types.Where(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)))
                        .Select(pair => pair.Key).ToList();
                _locker.ExitReadLock();
                if (models.Count>0)
                {
                    found=true;
                    var reqData = await ExtractParts(context);
                    foreach (Type model in models)
                    {
                        IEnumerable<ASecurityCheck> checks = Array.Empty<ASecurityCheck>();
                        _locker.EnterReadLock();
                        if (!_securityChecks.ContainsKey(model))
                            _securityChecks.Add(model,model.GetCustomAttributes().OfType<ASecurityCheck>());
                        checks = _securityChecks[model];
                        _locker.ExitReadLock();
                        if (checks.Any(check=>!check.HasValidAccess(reqData,null,url,null)))
                            throw new InsecureAccessException();
                    }
                    DateTime modDate = DateTime.MinValue;
                    foreach (Type model in models)
                    {
                        FileInfo fi = new(model.Assembly.Location);
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
                    log?.LogTrace("Checking cache for existing js file for {}", url);
                    context.Response.ContentType= "text/javascript";
                    context.Response.StatusCode= 200;
                    _locker.EnterWriteLock();
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
                    _locker.ExitWriteLock();
                    if (ret == null && models.Count>0)
                    {
                        ret = GenerateCode(models, url, url.EndsWith(".mjs", StringComparison.InvariantCultureIgnoreCase));
                        _locker.EnterWriteLock();
                        if (!_keys.Contains(url))
                        {
                            log?.LogTrace("Caching generated js file for {}", url);
                            _keys.Add(url);
                            _cache.Set<string>(url, ret, RequestHandlerBase.CACHE_ENTRY_OPTIONS);
                        }
                        _locker.ExitWriteLock();
                    }
                    context.Response.Headers.Append("Last-Modified", modDate.ToUniversalTime().ToString("R"));
                    context.Response.Headers.Append("Cache-Control", "public");
                    await context.Response.WriteAsync(ret);
                }
            }
            if (!found)
                await _next(context);
        }

        private string GenerateCode(List<Type> models,string url,bool useModuleExtension)
        {
            SModelType[] amodels = new SModelType[models.Count];
            for (int x = 0; x<models.Count; x++)
                amodels[x] = new SModelType(models[x]);
            log?.LogTrace("No cached js file for {}, generating new...", url);
            WrappedStringBuilder builder = new(_compressAllJS || url.EndsWith(".min.js",StringComparison.InvariantCultureIgnoreCase)|| url.EndsWith(".min.mjs", StringComparison.InvariantCultureIgnoreCase));
            builder.AppendLine(@$"import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{_coreImportPath}';
import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from '{_vueImportPath}';
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.0'; }}");
            foreach (IBasicJSGenerator gen in _oneTimeInitialGenerators)
            {
                builder.AppendLine($"//START:{gen.GetType().Name}");
                gen.GeneratorJS(ref builder, _urlBase, amodels, useModuleExtension, log);
                builder.AppendLine($"//END:{gen.GetType().Name}");
            }
            foreach (SModelType model in amodels)
            {
                log?.LogTrace("Processing module {} for js url {}", model.Type.FullName, url);
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
                gen.GeneratorJS(ref builder, _urlBase, amodels,useModuleExtension,log);
                builder.AppendLine($"//END:{gen.GetType().Name}");
            }
            return builder.ToString();
        }

        protected override void InternalLoadTypes(List<Type> types)
        {
            _locker.EnterWriteLock();
            foreach (Type t in types)
            {
                ModelJSFilePath[] paths = (ModelJSFilePath[])t.GetCustomAttributes(typeof(ModelJSFilePath), false);
                if (paths != null && paths.Length > 0)
                {
                    _types.Remove(t);
                    _types.Add(t, paths);
                }
            }
            _locker.ExitWriteLock();
        }

        protected override void InternalUnloadTypes(List<Type> types)
        {
            ClearCache();
            _locker.EnterWriteLock();
            foreach (Type t in types)
            {
                _types.Remove(t);
            }
            _locker.ExitWriteLock();
        }

        public bool HandlesJSPath(string url)
        {
            var result = false;
            _locker.EnterReadLock();
            if (_types!=null)
                result = _types.Any(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)));
            _locker.ExitReadLock();
            return result;
        }
    }
}
