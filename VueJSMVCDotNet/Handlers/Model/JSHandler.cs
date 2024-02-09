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

            private IEnumerable<SModelType> linkedTypes;
            public IEnumerable<SModelType> LinkedTypes
            {
                get
                {
                    return linkedTypes ??= Properties.Where(pi => pi.CanRead)
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
            public readonly bool HasSave => SaveMethod!=null;
            public MethodInfo SaveMethod { get; private init; }
            public readonly bool HasUpdate => UpdateMethod!=null;
            public MethodInfo UpdateMethod { get; private init; }
            public readonly bool HasDelete => DeleteMethod!=null;
            public MethodInfo DeleteMethod { get; private init; }

            public SModelType(Type type)
            {
                Type = type;
                Properties=type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi=> pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id"
                                && !pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0);
                InstanceMethods=type.GetMethods(Constants.INSTANCE_METHOD_FLAGS).Where(mi=> mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                StaticMethods=type.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
                linkedTypes = null;
                SaveMethod = type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0);
                UpdateMethod=type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0);
                DeleteMethod=type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS).FirstOrDefault(mi => mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0);
            }

            public override readonly bool Equals(object obj)
            {
                return (obj is SModelType model && Type.FullName==model.Type.FullName)
                    || (obj is Type type && Type.FullName==type.FullName);
            }

            public override readonly int GetHashCode()
            {
                return Type.FullName.GetHashCode();
            }
        }

        private static readonly IBasicJSGenerator[] oneTimeInitialGenerators = new IBasicJSGenerator[]{
            new ParsersGenerator()
        };

        private static readonly IBasicJSGenerator[] oneTimeFinishGenerators = new IBasicJSGenerator[]{
            new FooterGenerator()
        };

        private static readonly IJSGenerator[] classGenerators = new IJSGenerator[]
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

        private readonly List<string> keys;
        private readonly IMemoryCache cache;
        private readonly ReaderWriterLockSlim locker;
        private readonly Dictionary<Type,ModelJSFilePath[]> types;
        private readonly Dictionary<Type, IEnumerable<ASecurityCheck>> securityChecks;
        private readonly string urlBase;
        private readonly string vueImportPath;
        private readonly string coreImportPath;
        private readonly bool compressAllJS;
        public JSHandler(string urlBase,string vueImportPath, string coreImportPath,
            RequestDelegate next, ISecureSessionFactory sessionFactory, delRegisterSlowMethodInstance registerSlowMethod, bool compressAllJS,IMemoryCache cache, ILogger log)
            : base(next, sessionFactory, registerSlowMethod, urlBase,log)
        {
            this.cache = cache;
            this.urlBase=urlBase;
            this.vueImportPath=vueImportPath;
            this.coreImportPath=coreImportPath;
            this.compressAllJS=compressAllJS;
            keys = new List<string>();
            securityChecks=new Dictionary<Type, IEnumerable<ASecurityCheck>>();
            types = new Dictionary<Type, ModelJSFilePath[]>();
            locker = new ReaderWriterLockSlim();
        }

        public override void ClearCache()
        {
            locker.EnterWriteLock();
            keys.ForEach(key => cache.Remove(key));
            keys.Clear();
            securityChecks.Clear();
            locker.ExitWriteLock();
        }

        public override async Task ProcessRequest(HttpContext context)
        {
            bool found = false;
            if (context.Request.Method.ToUpper()=="GET")
            {
                string url = CleanURL(context);
                IEnumerable<Type> models = Array.Empty<Type>();
                locker.EnterReadLock();
                if (types!=null)
                    models = types.Where(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)))
                        .Select(pair => pair.Key).ToList();
                locker.ExitReadLock();
                if (models.Any())
                {
                    found=true;
                    var reqData = await ExtractParts(context);
                    if (models.SelectMany(model =>
                        {
                            locker.EnterWriteLock();
                            if (!securityChecks.ContainsKey(model))
                                securityChecks.Add(model, model.GetCustomAttributes().OfType<ASecurityCheck>());
                            locker.ExitWriteLock();
                            locker.EnterReadLock();
                            var checks = securityChecks[model];
                            locker.ExitReadLock();
                            return checks;
                        }).Any(check => !check.HasValidAccess(reqData, null, url, null)))
                        throw new InsecureAccessException();
                    DateTime modDate = models.Select(model =>
                    {
                        FileInfo fi = new(model.Assembly.Location);
                        return (fi.Exists ? fi.LastWriteTime : DateTime.MinValue);
                    }).Max();
                    if (modDate == DateTime.MinValue)
                        modDate = ModelRequestHandler.StartTime;
                    if (context.Request.Headers.TryGetValue("If-Modified-Since",out var value)
                        && DateTime.Parse(value).ToString()==modDate.ToString())
                    {
                        context.Response.StatusCode = 304;
                        return;
                    }

                    string ret = null;
                    log?.LogTrace("Checking cache for existing js file for {}", url);
                    context.Response.ContentType= "text/javascript";
                    context.Response.StatusCode= 200;
                    locker.EnterWriteLock();
                    if (keys.Contains(url))
                    {
                        try
                        {
                            ret = cache.Get<string>(url);
                        }
                        catch (Exception)
                        {
                            keys.Remove(url);
                        }
                        if (ret==null)
                            keys.Remove(url);
                    }
                    locker.ExitWriteLock();
                    if (ret == null && models.Any())
                    {
                        ret = GenerateCode(models, url, url.EndsWith(".mjs", StringComparison.InvariantCultureIgnoreCase));
                        locker.EnterWriteLock();
                        if (!keys.Contains(url))
                        {
                            log?.LogTrace("Caching generated js file for {}", url);
                            keys.Add(url);
                            cache.Set<string>(url, ret, RequestHandlerBase.CACHE_ENTRY_OPTIONS);
                        }
                        locker.ExitWriteLock();
                    }
                    context.Response.Headers.Append("Last-Modified", modDate.ToUniversalTime().ToString("R"));
                    context.Response.Headers.Append("Cache-Control", "public");
                    await context.Response.WriteAsync(ret);
                }
            }
            if (!found)
                await next(context);
        }

        private string GenerateCode(IEnumerable<Type> models,string url,bool useModuleExtension)
        {
            var amodels = models.Select(mod => new SModelType(mod));
            log?.LogTrace("No cached js file for {}, generating new...", url);
            WrappedStringBuilder builder = new(compressAllJS || url.EndsWith(".min.js",StringComparison.InvariantCultureIgnoreCase)|| url.EndsWith(".min.mjs", StringComparison.InvariantCultureIgnoreCase));
            builder.AppendLine(@$"import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{coreImportPath}';
import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from '{vueImportPath}';
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.0'; }}");
            //generate one times
            oneTimeInitialGenerators.ForEach(generator =>
            {
                builder.AppendLine($"//START:{generator.GetType().Name}");
                generator.GeneratorJS(builder, urlBase, amodels, useModuleExtension, log);
                builder.AppendLine($"//END:{generator.GetType().Name}");
            });

            //generate class items
            amodels.ForEach(model =>
            {
                log?.LogTrace("Processing module {} for js url {}", model.Type.FullName, url);
                classGenerators.ForEach(generator =>
                {
                    builder.AppendLine($"//START:{generator.GetType().Name}");
                    generator.GeneratorJS(builder, model, urlBase, log);
                    builder.AppendLine($"//END:{generator.GetType().Name}");
                });
            });

            //generate finishers
            oneTimeFinishGenerators.ForEach(generator =>
            {
                builder.AppendLine($"//START:{generator.GetType().Name}");
                generator.GeneratorJS(builder, urlBase, amodels, useModuleExtension, log);
                builder.AppendLine($"//END:{generator.GetType().Name}");
            });

            return builder.ToString();
        }

        protected override void InternalLoadTypes(List<Type> types)
        {
            locker.EnterWriteLock();
            types.ForEach(t =>
            {
                ModelJSFilePath[] paths = (ModelJSFilePath[])t.GetCustomAttributes(typeof(ModelJSFilePath), false);
                if (paths != null && paths.Length > 0)
                {
                    this.types.Remove(t);
                    this.types.Add(t, paths);
                }
            });
            locker.ExitWriteLock();
        }

        protected override void InternalUnloadTypes(List<Type> types)
        {
            ClearCache();
            locker.EnterWriteLock();
            types.ForEach(t => this.types.Remove(t));
            locker.ExitWriteLock();
        }

        public bool HandlesJSPath(string url)
        {
            var result = false;
            locker.EnterReadLock();
            result = types?.Any(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)))??false;
            locker.ExitReadLock();
            return result;
        }

        internal static void ExtractPropertyType(Type type, out bool array, out Type propType)
        {
            propType = type;
            array = false;
            if (propType.FullName.StartsWith("System.Nullable"))
            {
                if (propType.IsGenericType)
                    propType = propType.GetGenericArguments()[0];
                else
                    propType = propType.GetElementType();
            }
            if (propType.IsArray)
            {
                array = true;
                propType = propType.GetElementType();
            }
            else if (propType.IsGenericType)
            {
                if (propType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    array = true;
                    propType = propType.GetGenericArguments()[0];
                }
            }
        }
    }
}
