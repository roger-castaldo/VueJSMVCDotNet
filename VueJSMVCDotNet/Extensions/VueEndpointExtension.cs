using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VueJSMVCDotNet.Endpoints;
using VueJSMVCDotNet.Endpoints.DataSources;
using VueJSMVCDotNet.Endpoints.Filtering;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Extensions
{
    /// <summary>
    /// Houses the extensions for adding the proper services and endpoints when using this library
    /// </summary>
    public static class VueEndpointExtension
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        private const string defaultVueImportPath = "https://unpkg.com/vue@3/dist/vue.runtime.esm-browser.prod.js";
        private const string deafultVueLoaderImportPath = "https://unpkg.com/vue3-sfc-loader@0.9.5/dist/vue3-sfc-loader.esm.js";
#pragma warning restore S1075 // URIs should not be hardcoded
        private const string defaultCoreJSURL = "/VueJSMVCDotNet_core.min.js";

        /// <summary>
        /// Map the appropriate services into the service collection for using this library
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="logger">An ILogger instance if one is desired</param>
        /// <param name="vueImportPath">The override for the import path for vuejs if one is desired</param>
        /// <param name="coreJSURL">The override for the Core JS url if one is desired</param>
        /// <param name="coreJSImport">The override for the CoreJS import path if one is desired</param>
        /// <param name="ignoreInvalidModels">Indicates if invalid models/handlers should be ignored or an error thrown</param>
        /// <param name="compressJS">Indicates if all Javascript should be compressed</param>
        /// <param name="cache">An IMemoryCache implementation if one is desired as opposed to one existing in the service collection</param>
        /// <returns>The service collection as per extenion standards</returns>
        public static IServiceCollection UseVueJSMVCModels(this IServiceCollection services,
            ILogger? logger = null,
            string vueImportPath = defaultVueImportPath,
            string coreJSURL = defaultCoreJSURL,
            string? coreJSImport = null,
            bool ignoreInvalidModels = false,
            bool compressJS = true,
            IMemoryCache? cache = null
        )
            => services.AddSingleton<ModelsDataSource>((provider) => new(
                logger??provider.GetService<ILogger>(),
                vueImportPath,
                coreJSURL,
                coreJSImport??coreJSURL,
                ignoreInvalidModels,
                compressJS,
                cache??provider.GetService<IMemoryCache>()
            ))
            .AddSingleton<IModelDataSource>(x=>x.GetRequiredService<ModelsDataSource>())
            .AddSingleton<FeatureGateFilter>();

        /// <summary>
        /// Used to map the endpoints into the Endpoint Route Builder.
        /// </summary>
        /// <param name="builder">The EndpointRouteBuilder as per extension standards</param>
        /// <returns>The EndpointRouteBuilder as per extension standards</returns>
        public static IEndpointRouteBuilder MapVueJSMVSModels(this IEndpointRouteBuilder builder)
        {
            var source = builder.ServiceProvider.GetRequiredService<IModelDataSource>();
            source.AssemblyAdded();
            builder.DataSources.Add((ModelsDataSource)source);
            return builder;
        }

        /// <summary>
        /// Used to map the endpoints into the Endpoint Route Builder for the message files service if desired
        /// </summary>
        /// <param name="builder">The EndpointRouteBuilder as per extension standards</param>
        /// <param name="fileProvider">The file provider to use for mapping the message files</param>
        /// <param name="baseURL">The base path for all message files</param>
        /// <param name="compressJS">Indicates if all Javascript should be compressed</param>
        /// <param name="corePath">The import path for the core js</param>
        /// <param name="vueImportPath">The import path for vue js</param>
        /// <param name="logger">An ILogger instance if one is desired</param>
        /// <param name="cache">An IMemoryCache implementation if one is desired</param>
        /// <returns>The EndpointRouteBuilder as per extension standards</returns>
        public static IEndpointRouteBuilder UseVueJSMVCMessages(this IEndpointRouteBuilder builder,
            IFileProvider fileProvider,
            string baseURL,
            bool compressJS = true,
            string corePath = defaultCoreJSURL,
            string vueImportPath = defaultVueImportPath,
            ILogger? logger = null,
            IMemoryCache? cache = null)
        => new MessagesEndpoint(fileProvider, baseURL, compressJS, corePath, vueImportPath, logger??builder.ServiceProvider.GetService<ILogger>(), cache??builder.ServiceProvider.GetService<IMemoryCache>())
            .AddEndpoint(builder);

        /// <summary>
        /// Used to map the endpoints into the Endpoint Route Builder for the vue files service if desired
        /// </summary>
        /// <param name="builder">The EndpointRouteBuilder as per extension standards</param>
        /// <param name="fileProvider">The file provider to use for mapping the message files</param>
        /// <param name="baseURL">The base path for all vue files</param>
        /// <param name="vueImportPath">The import path for vue js</param>
        /// <param name="vueLoaderImportPath">The import path for the vue loader js</param>
        /// <param name="coreJSImport">The override for the CoreJS import path if one is desired</param>
        /// <param name="compressJS">Indicates if all Javascript should be compressed</param>
        /// <param name="logger">An ILogger instance if one is desired</param>
        /// <param name="cache">An IMemoryCache implementation if one is desired</param>
        /// <returns>The EndpointRouteBuilder as per extension standards</returns>
        public static IEndpointRouteBuilder UseVueJSMVCVueFiles(this IEndpointRouteBuilder builder,
            IFileProvider fileProvider,
            string baseURL,
            string vueImportPath = defaultVueImportPath,
            string vueLoaderImportPath = deafultVueLoaderImportPath,
            string coreJSImport = defaultCoreJSURL,
            bool compressJS = true,
            ILogger? logger = null,
            IMemoryCache? cache = null)
        => new VueFilesEndpoint(fileProvider, baseURL, vueImportPath, vueLoaderImportPath, coreJSImport, compressJS, (path) => CheckModelPath(builder, path), logger??builder.ServiceProvider.GetService<ILogger>(), cache??builder.ServiceProvider.GetService<IMemoryCache>())
            .AddEndpoint(builder);

        private static bool CheckModelPath(IEndpointRouteBuilder builder, string path)
            => builder.DataSources.AsQueryable()
                .OfType<ModelsDataSource>()
                .FirstOrDefault()?.Endpoints.OfType<RouteEndpoint>().Any(re => path.Equals(re.RoutePattern.ToString(), StringComparison.InvariantCultureIgnoreCase))
            ?? false;
    }
}
