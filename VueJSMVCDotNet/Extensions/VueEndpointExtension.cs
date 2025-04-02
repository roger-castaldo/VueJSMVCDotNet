using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using VueJSMVCDotNet.Endpoints;
using VueJSMVCDotNet.Endpoints.DataSources;

namespace VueJSMVCDotNet.Extensions
{
    public static class VueEndpointExtension
    {
        private const string defaultVueImportPath = "https://unpkg.com/vue@3/dist/vue.runtime.esm-browser.prod.js";
        private const string deafultVueLoaderImportPath = "https://unpkg.com/vue3-sfc-loader@0.9.5/dist/vue3-sfc-loader.esm.js";
        private const string defaultCoreJSURL = "/VueJSMVCDotNet_core.min.js";

        public static IServiceCollection UseVueJSMVCModels(this IServiceCollection services,
            ILogger? logger = null,
            string vueImportPath = defaultVueImportPath,
            string coreJSURL = defaultCoreJSURL,
            string? coreJSImport = null,
            bool ignoreInvalidModels = false,
            bool compressJS = true,
            IMemoryCache? cache = null
        )
            => services.AddSingleton<ModelsDataSource>((provider)=>new(logger,
                vueImportPath,
                coreJSImport??coreJSURL,
                ignoreInvalidModels,
                compressJS,
                cache
            ));

        public static IEndpointRouteBuilder MapVueJSMVSModels(this IEndpointRouteBuilder builder)
        {
            var source = builder.ServiceProvider.GetRequiredService<ModelsDataSource>();
            source.AssemblyAdded();
            builder.DataSources.Add(source);
            return builder;
        }

        public static IEndpointRouteBuilder UseVueJSMVCMessages(this IEndpointRouteBuilder builder,
            IFileProvider fileProvider,
            string baseURL,
            bool compressJS = true,
            string corePath = defaultCoreJSURL,
            string vueImportPath = defaultVueImportPath,
            ILogger? logger = null,
            IMemoryCache? cache = null)
        => new MessagesEndpoint(fileProvider, baseURL, compressJS, corePath, vueImportPath, logger, cache)
            .AddEndpoint(builder);

        public static IEndpointRouteBuilder UseVueJSMVCVueFiles(this IEndpointRouteBuilder builder,
            IFileProvider fileProvider,
            string baseURL,
            string vueImportPath = defaultVueImportPath,
            string vueLoaderImportPath = deafultVueLoaderImportPath,
            string coreJSImport = defaultCoreJSURL,
            bool compressJS = true,
            ILogger? logger = null,
            IMemoryCache? cache = null)
        => new VueFilesEndpoint(fileProvider, baseURL,vueImportPath,vueLoaderImportPath,coreJSImport,compressJS,(path)=>CheckModelPath(builder,path),logger,cache)
            .AddEndpoint(builder);

        private static bool CheckModelPath(IEndpointRouteBuilder builder, string path)
        {
            var modelsDataSource = (ModelsDataSource?)builder.DataSources.AsQueryable().FirstOrDefault(eds => eds is ModelsDataSource);
            if (modelsDataSource != null)
                return modelsDataSource.Endpoints.OfType<RouteEndpoint>().Any(re => path.Equals(re.RoutePattern.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return false;
        }
    }
}
