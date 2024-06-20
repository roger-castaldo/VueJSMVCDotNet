using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Threading;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Handlers.Model.JSGenerators;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class JSHandler(string urlBase, string vueImportPath, string coreImportPath,
        ISecureSessionFactory sessionFactory, bool compressAllJS, ILogger log) : 
        ModelRequestHandlerBase(sessionFactory, urlBase, log),ICachingRequestHandler
    {
        public struct SModelType
        {
            public Type Type { get; private init; }
            public IEnumerable<PropertyInfo> Properties { get; private init; }
            public IEnumerable<MethodInfo> InstanceMethods { get; private init; }
            public IEnumerable<MethodInfo> StaticMethods { get; private init; }

            private IEnumerable<SModelType> linkedTypes;
            public IEnumerable<SModelType> LinkedTypes
            {
                get
                {
                    return linkedTypes ??= Properties.Where(pi => pi.CanRead)
                            .Select(pi => Utility.ExtractUnderlyingType(pi.PropertyType, out _, out _, out _))
                            .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                            .Select(t => new SModelType(t))
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi => Utility.ExtractUnderlyingType(mi.ReturnType, out _, out _, out _))
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
                Properties=type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0 && pi.Name != "id"
                                && !pi.PropertyType.FullName.Contains("+KeyCollection") && pi.GetGetMethod().GetParameters().Length == 0);
                InstanceMethods=type.GetMethods(Constants.INSTANCE_METHOD_FLAGS).Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0);
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
            new HeaderGenerator(),
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

        private readonly InternalChangeToken changeToken = new();
        private readonly ReaderWriterLockSlim locker = new();
        private readonly Dictionary<Type, ModelJSFilePath[]> types = [];

        private string GenerateCode(IEnumerable<Type> models, string url, bool useModuleExtension)
        {
            var amodels = models.Select(mod => new SModelType(mod));
            log?.LogTrace("No cached js file for {}, generating new...", url);
            WrappedStringBuilder builder = new(compressAllJS || url.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase)|| url.EndsWith(".min.mjs", StringComparison.InvariantCultureIgnoreCase));
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

        public bool HandlesJSPath(string url)
        {
            var result = false;
            locker.EnterReadLock();
            result = types?.Any(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)))??false;
            locker.ExitReadLock();
            return result;
        }

        protected override bool InternalHandlesRequest(HttpContext context, out object state, out string cacheURL)
        {
            state=null;
            cacheURL=null;
            if (string.Equals(context.Request.Method, "GET", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = CleanURL(context);
                locker.EnterReadLock();
                var locatedTypes = types?.Where(pair => pair.Value.Any(mjsfp => mjsfp.IsMatch(url)))
                    .Select(pair => pair.Key);
                locker.ExitReadLock();
                cacheURL=url;
                state = (locatedTypes?? []).Any() ? new ModelRequestState(locatedTypes, url) : null;
            }
            return state!=null;
        }

        public Task<ICachableResponse> ProduceResponseAsync(HttpContext context, object state)
        {
            var cachedState = (ModelRequestState)state;
            return Task.FromResult<ICachableResponse>(new CachableResponse(
                GenerateCode((IEnumerable<Type>)cachedState.State,cachedState.URL,cachedState.URL.EndsWith(".mjs",StringComparison.InvariantCultureIgnoreCase)),
                "text/javascript",
                DateTime.Now,
                [changeToken]
            ));
        }

        public override void LoadTypes(IEnumerable<Type> types)
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

        public override void UnloadTypes(IEnumerable<Type> types)
        {
            locker.EnterWriteLock();
            types.ForEach(t => this.types.Remove(t));
            locker.ExitWriteLock();
            changeToken.HasChanged=true;
        }

        public override void ClearTypes()
        {
            locker.EnterWriteLock();
            types.Clear();
            locker.ExitWriteLock();
        }
    }
}
