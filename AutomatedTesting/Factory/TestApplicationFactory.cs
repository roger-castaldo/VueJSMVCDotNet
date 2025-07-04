using AutomatedTesting.FileProvider;
using AutomatedTesting.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Collections.Generic;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Factory
{
    internal class TestApplicationFactory(bool ignoreInvalidModels, ILogger logWriter, IMemoryCache cache, IDataStore store, SecureSession session,Dictionary<string,string> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (settings!=null && settings.Count>0)
            {
                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(settings)
                    .Build();
                builder.UseConfiguration(configuration);
            }
            builder.ConfigureServices(services =>
            {
                services.AddFeatureManagement();
                if (cache!= null)
                    services.AddSingleton<IMemoryCache>(cache);
                services.AddSingleton<IFileProvider, EmbeddedResourceFileProvider>()
                .UseVueJSMVCModels(
                    logger:logWriter, 
                    vueImportPath: Constants.VUE_IMPORT_PATH, 
                    coreJSImport: Constants.CORE_IMPORT_PATH, 
                    ignoreInvalidModels: ignoreInvalidModels,
                    cache: cache,
                    compressJS: false)
                .AddSingleton<IDataStore>(store??new DataStore())
                .AddSingleton<ISecureSessionFactory>(session??new SecureSession());
            });
        }
    }
}
