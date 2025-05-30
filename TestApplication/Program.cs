using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TestApplication;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;

var compressJS = false;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddDistributedMemoryCache()
    .AddSession()
    .AddCors()
    .UseVueJSMVCModels(
        compressJS: compressJS,
        vueImportPath:"vue"
    )
    .AddSingleton<ISecureSessionFactory>(new SessionManager());
var app = builder.Build();

// Register the dynamic endpoint data source
app.UseDefaultFiles()
    .UseStaticFiles()
    .UseCookiePolicy()
    .UseSession()
    .UseRouting()
    .UseEndpoints(endpoints => endpoints.MapVueJSMVSModels()
        .UseVueJSMVCMessages(builder.Environment.WebRootFileProvider,"/resources/messages", compressJS: compressJS, vueImportPath:"vue")
        .UseVueJSMVCVueFiles(builder.Environment.WebRootFileProvider,"/resources/vueFiles",compressJS: compressJS, vueImportPath:"vue")
    );

await app.RunAsync();
