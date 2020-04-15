using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Org.Reddragonit.VueJSMVCDotNet;

namespace TestApplication
{
    public class Startup
    {
        private RequestHandler _vueReqesthandler;

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            _vueReqesthandler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            
            app.MapWhen(context => _vueReqesthandler.HandlesRequest(context), builder =>
            {
                builder.UseSession();
                builder.Run(context => _vueReqesthandler.ProcessRequest(context, new SessionManager(context)));
            });
        }
    }
}
