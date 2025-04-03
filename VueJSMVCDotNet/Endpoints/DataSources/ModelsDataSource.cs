using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Threading;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;

namespace VueJSMVCDotNet.Endpoints.DataSources
{
    internal class ModelsDataSource(ILogger? logger,
            string vueImportPath,
            string coreJSImport,
            bool ignoreInvalidModels,
            bool compressJS,
            IMemoryCache? cache) : EndpointDataSource, IDisposable
    {
        private static readonly Type[] ModelEndpoints = [.. typeof(AModelEndpoint<,>)
            .Assembly
            .GetTypes()
            .Where(handler => !handler.IsAbstract
                && !handler.IsInterface
                && handler.IsGenericType
                && handler.BaseType!=null
                && handler.BaseType.IsGenericType
                && Equals(handler.BaseType.GetGenericTypeDefinition(),typeof(AModelEndpoint<,>)))
        ];

        private CancellationTokenSource tokenSource = new();
        private bool disposedValue;
        private readonly ReaderWriterLockSlim locker = new();
        private readonly Dictionary<Type, IEnumerable<IEndpointHandler>> endpoints = [];
        private string? compressedCore = null;

        internal string? GetModelImportURL(Type modelType)
        {
            locker.EnterReadLock();
            var result = endpoints.Keys
                .FirstOrDefault(t => Equals(modelType, t.GetInterfaces().First(t => t.IsGenericType && Equals(t.GetGenericTypeDefinition(), typeof(IModelHandler<>))).GetGenericArguments()[0]))
                ?.GetCustomAttributes<ModelRouteAttribute>()
                ?.First()
                ?.Path;
            locker.ExitReadLock();
            return result;
        }
        internal IModelHandler<M>? GetModelHandlerType<M>(IServiceProvider serviceProvider)
            where M : IModel
        {
            locker.EnterReadLock();
            var result = endpoints.Keys
                .FirstOrDefault(t => Equals(typeof(M), t.GetInterfaces().First(t => t.IsGenericType && Equals(t.GetGenericTypeDefinition(), typeof(IModelHandler<>))).GetGenericArguments()[0]));
            locker.ExitReadLock();
            return (result==null ? null : (IModelHandler<M>)ActivatorUtilities.CreateInstance(serviceProvider, result!));
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                locker.EnterReadLock();
                if (compressedCore==null)
                {
                    using StreamReader sr = new(typeof(ModelsDataSource).Assembly.GetManifestResourceStream("VueJSMVCDotNet.Endpoints.Model.JSGenerators.core.js"));
                    compressedCore = JSMinifier.Minify($@"import * as vue from ""{vueImportPath}"";
{sr.ReadToEnd()}");
                    sr.Close();
                }
                var results = endpoints.Values.SelectMany(g => g.SelectMany(m => m.AsEndpoints))
                    .Append(new(
                        requestDelegate: async (context) =>
                        {
                            context.Response.ContentType = "text/javascript";
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync(compressedCore);
                        },
                        routePattern: RoutePatternFactory.Parse(coreJSImport),
                        order: 0,
                        metadata: new(
                            new HttpMethodMetadata([HttpMethods.Get])
                        ),
                        displayName: "Core JS path"
                    ))
                    .ToArray();
                locker.ExitReadLock();
                return results;
            }
        }

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(tokenSource.Token);

        private void TriggerChange()
        {
            tokenSource.Cancel();  // Notify ASP.NET Core that the routes have changed
            tokenSource = new();  // Reset token for future changes
        }

        internal void UnloadAssemblyContext(string? contextName)
        {
            var types = Utility.UnloadAssemblyContext(contextName)?? [];
            locker.EnterWriteLock();
            foreach (var type in types)
                endpoints.Remove(type);
            locker.ExitWriteLock();
            TriggerChange();
        }

        internal void AssemblyAdded()
        {
            locker.EnterWriteLock();
            endpoints.Clear();
            locker.ExitWriteLock();
            AssemblyLoadContext.All
                .ForEach(alc => AsssemblyLoadContextAdded(alc, false));
            TriggerChange();
        }

        internal void AsssemblyLoadContextAdded(string? contextName)
        {
            var alc = AssemblyLoadContext.All.FirstOrDefault(alc => string.Equals(alc.Name, contextName, StringComparison.InvariantCultureIgnoreCase));
            if (alc!=null)
                AsssemblyLoadContextAdded(alc);
        }

        internal void AsssemblyLoadContextAdded(AssemblyLoadContext alc, bool triggerChange = true)
        {
            logger?.LogDebug("Loading Assembly Load Context {Name}", alc.Name);
            IEnumerable<Exception> errors = DefinitionValidator.Validate(alc, logger, out var invalidModels, out var models);
            if (errors.Any())
            {
                logger?.LogError("Validation errors:");
                errors.ForEach(e => logger?.LogError(e, "Validation Error: {Message}", e.Message));
                logger?.LogError("Invalid IModelHandlers:");
                invalidModels.ForEach(t => logger?.LogError("Invalid IModelHandler: {FullName}", t.HandlerType.FullName));
            }
            if (errors.Any() && !ignoreInvalidModels)
                throw new ModelValidationException(errors);
            models = models.Where(m => !invalidModels.Contains(m));
            locker.EnterWriteLock();
            try
            {
                foreach (var handler in models)
                {
                    var producedEndpoints = new List<IEndpointHandler>();
                    foreach (var endpointType in ModelEndpoints)
                        producedEndpoints.Add((IEndpointHandler)Activator.CreateInstance(endpointType.MakeGenericType(handler.HandlerType, handler.ModelType), logger)!);
                    producedEndpoints.Add((IEndpointHandler)Activator.CreateInstance(typeof(JSEndpoint<,>).MakeGenericType(handler.HandlerType, handler.ModelType), vueImportPath, coreJSImport, compressJS, this, logger, cache));
                    endpoints.Add(handler.HandlerType, producedEndpoints);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            locker.ExitWriteLock();
            if (triggerChange)
                TriggerChange();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    locker.EnterWriteLock();
                    endpoints.Clear();
                    locker.ExitWriteLock();
                    locker.Dispose();
                    tokenSource.Dispose();
                }
                disposedValue=true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
