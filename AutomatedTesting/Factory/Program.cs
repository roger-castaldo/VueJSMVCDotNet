using AutomatedTesting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using VueJSMVCDotNet.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
app.UseRouting()
    .UseEndpoints(endpoints =>
        endpoints.MapVueJSMVSModels()
        .UseVueJSMVCMessages(app.Services.GetRequiredService<IFileProvider>(), "/resources/messages", corePath: Constants.CORE_IMPORT_PATH, 
            vueImportPath: Constants.VUE_IMPORT_PATH, logger: app.Services.GetService<ILogger>(), cache: app.Services.GetService<IMemoryCache>(),
            compressJS: false)
        .UseVueJSMVCVueFiles(app.Services.GetRequiredService<IFileProvider>(), "/resources/vueFiles", 
            logger: app.Services.GetService<ILogger>(), 
            cache: app.Services.GetService<IMemoryCache>(),
            compressJS: false)
    );

app.Run();

public partial class Program { }