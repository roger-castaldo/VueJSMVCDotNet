using AutomatedTesting.FileProvider;
using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Jint;
using Jint.Native;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.IO;

namespace AutomatedTesting
{
    internal static class Utility
    {

        public static MemoryStream ExecuteRequest(string method,string path, VueMiddleware middleware,out int responseStatus,SecureSession session=null,object parameters=null)
        {
            MemoryStream ms = new MemoryStream();
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            if (session!=null)
                session.LinkToRequest(context);
            if (path.Contains("?"))
            {
                context.Request.Path = new PathString(path.Substring(0,path.IndexOf("?")));
                context.Request.QueryString = new QueryString(path.Substring(path.IndexOf("?")));
            }
            else
                context.Request.Path = new PathString(path);
            context.Response.Body = ms;

            if (parameters != null)
            {
                context.Request.ContentType = "text/json";
                context.Request.Body = new MemoryStream();
                StreamWriter sw = new StreamWriter(context.Request.Body);
                sw.Write(JSON.JsonEncode(parameters));
                sw.Flush();
                context.Request.Body.Position = 0;
            }
            middleware.InvokeAsync(context).Wait();
            responseStatus = context.Response.StatusCode;
            ms.Position = 0;
            return ms;
        }

        internal static string ReadResponse(MemoryStream ms)
        {
            string content = new StreamReader(ms).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            return content;
        }

        public static object ReadJSONResponse(MemoryStream ms)
        {
            return JSON.JsonDecode(ReadResponse(ms));
        }

        private const string _VUE_IMPORT_PATH = "vue";
        private const string _VUE_LOADER_PATH = "vue-loader";
        private static readonly EmbeddedResourceFileProvider _fileProvider = new EmbeddedResourceFileProvider();

        public static EmbeddedResourceFileProvider FileProvider { get { return _fileProvider; } }

        public static VueMiddleware CreateMiddleware(bool ignoreInvalidModels,bool blockFileProvider=false)
        {
            return new VueMiddleware(null, new VueMiddlewareOptions(
                modelsOptions: new VueModelsOptions(new SecureSession(), ignoreInvalidModels: ignoreInvalidModels),
                vueImportPath: _VUE_IMPORT_PATH,
                vueLoaderImportPath: _VUE_LOADER_PATH,
                fileProvider:(blockFileProvider ? null : _fileProvider),
                messageOptions: new MessageHandlerOptions("/resources/messages"),
                vueFilesOptions:new VueFilesHandlerOptions("/resources/vueFiles")
            ));
        }

        public static Engine CreateEngine()
        {
            Options opt = new Options();
            opt.EnableModules(typeof(Utility).Assembly.Location.Substring(0, typeof(Utility).Assembly.Location.LastIndexOf(Path.DirectorySeparatorChar)));
            Engine engine = new Engine(opt);
            StreamReader sr = new StreamReader(new FileStream("./resources/vue.esm-browser.prod.js", FileMode.Open, FileAccess.Read, FileShare.Read));
            engine.AddModule(
                _VUE_IMPORT_PATH, 
                sr.ReadToEnd()
            );
            sr.Close();
            sr = new StreamReader(new FileStream("./resources/vue3-sfc-loader.esm.js", FileMode.Open, FileAccess.Read, FileShare.Read));
            engine.AddModule(
                _VUE_LOADER_PATH,
                sr.ReadToEnd()
            );
            sr.Close();
            return engine;
        }
    }
}
