using Microsoft.AspNetCore.Http;
using System.Threading;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Handlers.Base;
using VueJSMVCDotNet.Handlers.Model.JSGenerators;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model
{
    internal class JSHandler(string? urlBase, string vueImportPath, string coreImportPath,
        ISecureSessionFactory? sessionFactory, bool compressAllJS, ILogger? Log) :
        ModelRequestHandlerBase(sessionFactory, urlBase, Log), ICachingRequestHandler
    {
        public struct SModelType(Type type)
        {
            public readonly Type Type
                => type;
            public readonly IEnumerable<PropertyInfo> Properties
                => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(pi => pi.GetCustomAttribute<ModelIgnorePropertyAttribute>(false)==null
                        && !Equals(pi.Name, "id")
                        && !(pi.PropertyType.FullName?.Contains("+KeyCollection")??false)
                        && (pi.GetGetMethod()?.GetParameters()?? []).Length == 0);
            public readonly IEnumerable<MethodInfo> InstanceMethods
                => type.GetMethods(Constants.INSTANCE_METHOD_FLAGS)
                    .Where(mi => mi.GetCustomAttribute<ExposedMethodAttribute>(false)!=null);
            public readonly IEnumerable<MethodInfo> StaticMethods
                => type.GetMethods(Constants.STATIC_INSTANCE_METHOD_FLAGS)
                    .Where(mi => mi.GetCustomAttribute<ExposedMethodAttribute>(false)!=null);

            private IEnumerable<SModelType>? linkedTypes = null;
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
                                .Select(mi => ((ExposedMethodAttribute)mi.GetCustomAttributes(typeof(ExposedMethodAttribute), false)[0]).ArrayElementType)
                                .Where(t => t!=null && t.GetInterfaces().Contains(typeof(IModel)))
                                .Select(t => new SModelType(t!))
                            )
                            .Distinct();
                }
            }
            public readonly bool HasSave
                => SaveMethod!=null;
            public readonly MethodInfo? SaveMethod
                => Array.Find(
                        type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS),
                        mi => mi.GetCustomAttribute<ModelSaveMethodAttribute>(false)!=null
                    );
            public readonly bool HasUpdate
                => UpdateMethod!=null;
            public readonly MethodInfo? UpdateMethod
                => Array.Find(
                        type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS),
                        mi => mi.GetCustomAttribute<ModelUpdateMethodAttribute>(false)!=null
                    );
            public readonly bool HasDelete
                => DeleteMethod!=null;
            public readonly MethodInfo? DeleteMethod
                => Array.Find(
                        type.GetMethods(Constants.STORE_DATA_METHOD_FLAGS),
                        mi => mi.GetCustomAttribute<ModelDeleteMethodAttribute>(false)!=null
                    );

            public override readonly bool Equals(object? obj)
            {
                return (obj is SModelType model && Equals(type.FullName, model.Type.FullName))
                    || (obj is Type etype && Equals(type.FullName, etype.FullName));
            }

            public override readonly int GetHashCode()
                => type.FullName?.GetHashCode()??int.MaxValue;
        }

        private static readonly IEnumerable<IBasicJSGenerator> OneTimeInitialGenerators = [
                new HeaderGenerator(),
                new ParsersGenerator()
            ];

        private static readonly IEnumerable<IBasicJSGenerator> OneTimeFinishGenerators = [
            new FooterGenerator()
        ];

        private static readonly IEnumerable<IJSGenerator> ClassGenerators = [
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
        ];

        private readonly InternalChangeToken changeToken = new();
        private readonly ReaderWriterLockSlim locker = new();
        private readonly Dictionary<Type, ModelJSFilePathAttribute[]> types = [];

        private string GenerateCode(IEnumerable<Type> models, string url, bool useModuleExtension)
        {
            var amodels = models.Select(mod => new SModelType(mod));
            Log?.LogTrace("No cached js file for {URL}, generating new...", url);
            WrappedStringBuilder builder = new(compressAllJS || url.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase)|| url.EndsWith(".min.mjs", StringComparison.InvariantCultureIgnoreCase));
            builder.AppendLine(@$"import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{coreImportPath}';
import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from '{vueImportPath}';
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.0'; }}");
            //generate one times
            OneTimeInitialGenerators.ForEach(generator =>
            {
                builder.AppendLine($"//START:{generator.GetType().Name}");
                generator.GeneratorJS(builder, URLBase, amodels, useModuleExtension, Log);
                builder.AppendLine($"//END:{generator.GetType().Name}");
            });

            //generate class items
            amodels.ForEach(model =>
            {
                Log?.LogTrace("Processing module {TypeName} for js url {URL}", model.Type.FullName, url);
                ClassGenerators.ForEach(generator =>
                {
                    builder.AppendLine($"//START:{generator.GetType().Name}");
                    generator.GeneratorJS(builder, model, URLBase, Log);
                    builder.AppendLine($"//END:{generator.GetType().Name}");
                });
            });

            //generate finishers
            OneTimeFinishGenerators.ForEach(generator =>
            {
                builder.AppendLine($"//START:{generator.GetType().Name}");
                generator.GeneratorJS(builder, URLBase, amodels, useModuleExtension, Log);
                builder.AppendLine($"//END:{generator.GetType().Name}");
            });

            return builder.ToString();
        }

        public bool HandlesJSPath(string url)
        {
            var result = false;
            locker.EnterReadLock();
            result = types?.Any(pair => Array.Exists(pair.Value, mjsfp => mjsfp.IsMatch(url)))??false;
            locker.ExitReadLock();
            return result;
        }

        protected override bool InternalHandlesRequest(HttpContext context, out object? state, out string? cacheURL)
        {
            state=null;
            cacheURL=null;
            if (string.Equals(context.Request.Method, "GET", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = CleanURL(context);
                locker.EnterReadLock();
                var locatedTypes = types?.Where(pair => Array.Exists(pair.Value, mjsfp => mjsfp.IsMatch(url)))
                    .Select(pair => pair.Key);
                locker.ExitReadLock();
                cacheURL=url;
                state = (locatedTypes?? []).Any() ? new ModelRequestState(locatedTypes!, url) : null;
            }
            return state!=null;
        }

        public Task<ICachableResponse?> ProduceResponseAsync(HttpContext context, object? state)
        {
            var cachedState = (ModelRequestState)state!;
            return Task.FromResult<ICachableResponse?>(new CachableResponse(
                GenerateCode((IEnumerable<Type>)cachedState.State, cachedState.URL, cachedState.URL.EndsWith(".mjs", StringComparison.InvariantCultureIgnoreCase)),
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
                ModelJSFilePathAttribute[] paths = (ModelJSFilePathAttribute[])t.GetCustomAttributes(typeof(ModelJSFilePathAttribute), false);
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
