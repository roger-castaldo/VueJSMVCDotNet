using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication.Handlers
{
    public class VueComponentMiddlewareOptions
    {
        public DirectoryInfo WebPath { get; private init; }
        public string BasePath { get; private init; }

        public VueComponentMiddlewareOptions(DirectoryInfo webPath, string basePath)
        {
            WebPath=webPath;
            BasePath=basePath.ToLower();
        }
    }

    public class VueComponentMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly VueComponentMiddlewareOptions _options;

        private static string ReadFile(DirectoryInfo di,FileInfo fi)
        {
            StreamReader sr = new(fi.OpenRead());
            string ret = sr.ReadToEnd().Trim();
            sr.Close();
            ret = ret.Replace("$template$", string.Format("templates['{0}']", fi.Name[..fi.Name.LastIndexOf('.')].Replace(".", "-").Replace(" ", "-")));
            ret = ret.Replace("$componentname$", string.Format(@"{0}-{1}", new object[] { di.Name, fi.Name[..fi.Name.LastIndexOf('.')].Replace(".", "-").Replace(" ", "-") }));
            return ret;
        }

        public VueComponentMiddleware(RequestDelegate next, VueComponentMiddlewareOptions options)
        {
            _next=next;
            _options=options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isHandling = false;
            if (context.Request.Method=="GET" && context.Request.Path.ToString().ToLower().StartsWith(_options.BasePath))
            {
                DirectoryInfo di = new DirectoryInfo(_options.WebPath.FullName + context.Request.Path.ToString()[..^(context.Request.Path.ToString().EndsWith(".min.js") ? 7 : 3)]);
                if (di.Exists)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("var templates={};");
                    foreach (FileInfo fi in di.GetFiles("*.template"))
                    {
                        sb.Append(string.Format("templates['{0}']='", fi.Name[..fi.Name.LastIndexOf('.')].Replace(".", "-").Replace(" ", "-")));
                        string cont = ReadFile(di, fi);
                        cont = cont.Replace("'", "\\'").Replace("\r\n", "\\\n");
                        sb.Append(cont);
                        sb.AppendLine("';");
                    }
                    foreach (FileInfo fi in di.GetFiles("*.json"))
                    {
                        sb.AppendLine(string.Format(@"Vue.component('{0}-{1}',", new object[] { di.Name, fi.Name[..fi.Name.LastIndexOf('.')].Replace(".", "-").Replace(" ", "-") }));
                        sb.AppendLine(ReadFile(di, fi));
                        sb.AppendLine(");");
                    }
                    foreach (FileInfo fi in di.GetFiles("*.js"))
                        sb.AppendLine(ReadFile(di, fi));
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/javascript";
                    await context.Response.WriteAsync(sb.ToString());
                }
            }
            if (!isHandling)
                await _next(context);
        }
    }

    public static class VueComponentMiddlewareExtensions
    {
        public static IApplicationBuilder UseVueComponentMiddleware(
            this IApplicationBuilder builder,
            VueComponentMiddlewareOptions options
            )
        {
            return builder.UseMiddleware<VueComponentMiddleware>(options);
        }
    }
}
