using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet;
using TestApplication.Handlers;

namespace TestApplication
{
    public class Startup
    {

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseVueMiddleware(new VueMiddlewareOptions(
                vueImportPath: "vue",
                modelsOptions:new VueModelsOptions(new SessionManager(), baseURL: "testing"),
                messageOptions:new MessageHandlerOptions(env.WebRootFileProvider,"/resources/messages"))
            );
            app.UseVueComponentMiddleware(new VueComponentMiddlewareOptions(new System.IO.DirectoryInfo(env.WebRootPath), "/resources/components"));
        }
    }
}
