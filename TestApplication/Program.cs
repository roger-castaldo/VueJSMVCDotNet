using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.AspNetCore;
using Microsoft.FeatureManagement.Mvc;
using TestApplication;
using VueJSMVCDotNet.Extensions;
using VueJSMVCDotNet.Interfaces;
//add in feature gate to test things
var compressJS = false;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json");

builder.Services
    .AddFeatureManagement();

builder.Services
    .AddDistributedMemoryCache()
    .AddSession()
    .AddCors()
    .UseVueJSMVCModels(
        compressJS: compressJS,
        vueImportPath: "vue"
    )
    .AddSingleton<ISecureSessionFactory>(new SessionManager());


var app = builder.Build();

// Register the dynamic endpoint data source
app.UseDefaultFiles()
    .UseStaticFiles()
    .UseCookiePolicy()
    .UseSession()
    .UseRouting()
    .UseEndpoints(endpoints =>
    {
        endpoints
            .MapVueJSMVSModels()
            .UseVueJSMVCMessages(builder.Environment.WebRootFileProvider, "/resources/messages", compressJS: compressJS, vueImportPath: "vue")
            .UseVueJSMVCVueFiles(builder.Environment.WebRootFileProvider, "/resources/vueFiles", compressJS: compressJS, vueImportPath: "vue");
        endpoints.MapGet("/test1", () => "Feature is on!")
            .WithFeatureGate("PersonSearchAllowed");
        endpoints.MapGet("/test2", () => "Feature is on!")
            .WithMetadata(new FeatureGateAttribute("PersonSearchAllowed"));
    });

await app.RunAsync();
