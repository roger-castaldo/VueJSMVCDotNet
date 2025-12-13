using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Caching;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
using VueJSMVCDotNet.Interfaces.Internal;
using VueJSMVCDotNet.Javascript;

namespace VueJSMVCDotNet.Endpoints.Model
{
    internal class JSEndpoint<H, M>(string vueImportPath, string coreImportPath, bool compressAllJS, ModelsDataSource modelsDataSource, ILogger? logger, IMemoryCache? cache) :
        ACachingEndpoint(logger, cache), IEndpointHandler
        where H : IModelHandler<M>
        where M : IModel
    {
        private const string ExtensionKey = "ext";
        private const string BaseURLKey = "_JSBaseURL";

        private static readonly IEnumerable<IGenerator> Generators = [
            new ParsersGenerator(),
            new ModelClassHeaderGenerator(),
            new JSONGenerator(),
            new ModelDefaultMethodsGenerator(),
            new ParseGenerator(),
            new ModelInstanceFooterGenerator(),
            new ModelLoadAllGenerator(),
            new ModelLoadGenerator(),
            new MethodsGenerator(),
            new EventStreamsGenerator(),
            new ModelListCallGenerator(),
            new ModelClassFooterGenerator(),
            new FooterGenerator()
        ];

        private readonly ModelType modelType = new ModelType(typeof(M), typeof(H), (type) => modelsDataSource.GetModelImportURL(type));
        private readonly string importHeader = @$"import {{isString, isFunction, cloneData, ajax, isEqual, checkProperty, stripBigInt, EventHandler, ModelList, ModelMethods}} from '{coreImportPath}';
import {{ version, createApp, isProxy, toRaw, reactive, readonly, ref }} from '{vueImportPath}';
if (version===undefined || version.indexOf('3')!==0){{ throw 'Unable to operate without Vue version 3.x'; }}
{Constants.HOST_URL_CONSTRUCTOR}";

        IEnumerable<Endpoint> IEndpointHandler.AsEndpoints
            => typeof(H)
                .GetCustomAttributes<ModelRouteAttribute>()
            .Select(mra => BuildEndpoint<H, M>(
                requestDelegate: (context) =>
                {
                    context.Items[BaseURLKey] = mra.Path;
                    return ExecuteRequestAsync(context);
                },
                routePattern: RoutePatternFactory.Parse($"{mra.Path}.{{{ExtensionKey}:regex(^mjs|min\\.mjs|min\\.js|js$)}}"),
                order: 0,
                displayName: $"JS call for {typeof(M).Name}",
                httpMethods: [HttpMethods.Get],
                methods: []
            ));

        protected override async Task<CachableResponse?> ProduceCachableResponseAsync(HttpContext context)
        {
            var baseURL = (string?)context.Items[BaseURLKey]??string.Empty;
            var useModuleExtension = ((string?)context.Request.RouteValues[ExtensionKey])?.EndsWith("mjs", StringComparison.InvariantCultureIgnoreCase)??false;
            var isMin = ((string?)context.Request.RouteValues[ExtensionKey])?.StartsWith("min", StringComparison.InvariantCultureIgnoreCase)??false;
            var builder = new StringBuilder();
            builder.AppendLine(importHeader);
            Generators.ForEach(gen =>
            {
                builder.AppendLine($"//START:{gen.GetType().Name}");
                if (gen is IBaseJSGenerator baseJSGenerator)
                    baseJSGenerator.GeneratorJS(builder, modelType, baseURL, useModuleExtension, isMin, Logger);
                else if (gen is IJSGenerator jsGenerator)
                    jsGenerator.GeneratorJS(builder, modelType, baseURL, Logger);
                builder.AppendLine($"//END:{gen.GetType().Name}");
            });

            return new(
                (compressAllJS||isMin ? await context.RequestServices.GetRequiredService<JSEngine>().CompressCodeAsync(builder.ToString()) : builder.ToString()),
                "text/javascript",
                DateTime.UtcNow,
                []
            );
        }
    }
}
