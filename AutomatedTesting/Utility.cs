using AutomatedTesting.FileProvider;
using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AutomatedTesting
{
    internal static class Utility
    {

        public static MemoryStream ExecuteRequest(string method, string path, VueMiddleware middleware, out int responseStatus, SecureSession session = null, object parameters = null, Dictionary<string, string> headers = null,IDataStore store=null)
        {
            IHeaderDictionary responseHeaders;
            return _ExecuteRequestExportingHeaders(method, path, middleware, out responseStatus, out responseHeaders, session: session, parameters: parameters, headers: headers,store:store);
        }

        public static MemoryStream ExecuteRequest(string method, string path, VueMiddleware middleware, out int responseStatus, Dictionary<string, StringValues> formData, SecureSession session = null, Dictionary<string, string> headers = null, IDataStore store = null)
        {
            IHeaderDictionary responseHeaders;
            return _ExecuteRequestExportingHeaders(method, path, middleware, out responseStatus, out responseHeaders, session: session, formData:formData, headers: headers,store:store);
        }

        public static MemoryStream ExecuteRequestExportingHeaders(string method, string path, VueMiddleware middleware, out int responseStatus,out IHeaderDictionary responseHeaders, SecureSession session = null, object parameters = null, Dictionary<string, string> headers = null, IDataStore store = null)
        {
            return _ExecuteRequestExportingHeaders(method,path,middleware,out responseStatus,out responseHeaders,session:session,parameters:parameters, headers:headers,store:store);
        }

        private static MemoryStream _ExecuteRequestExportingHeaders(string method, string path, VueMiddleware middleware, out int responseStatus, out IHeaderDictionary responseHeaders, SecureSession session = null, object parameters = null, Dictionary<string, string> headers = null,Dictionary<string, StringValues> formData=null, IDataStore store = null)
        {
            MemoryStream ms = new MemoryStream();
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Features.Set<IDataStore>(store??new DataStore());
            if (session!=null)
                session.LinkToRequest(context);
            if (path.Contains("?"))
            {
                context.Request.Path = new PathString(path.Substring(0, path.IndexOf("?")));
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
            else if (formData!=null)
            {
                context.Request.ContentType="multipart/form-data";
                context.Request.Form = new FormCollection(formData);
            }

            if (headers != null)
            {
                foreach (string key in headers.Keys)
                    context.Request.Headers.Append(key, headers[key]);
            }

            middleware.InvokeAsync(context).Wait();
            responseStatus = context.Response.StatusCode;
            ms.Position = 0;
            responseHeaders = context.Response.Headers;

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

        public static VueMiddleware CreateMiddleware(bool ignoreInvalidModels, bool blockFileProvider = false, ILogger logWriter = null, string[] securityHeaders = null)
        {
            return new VueMiddleware(null, new VueMiddlewareOptions() {
                VueModelsOptions= new VueModelsOptions(){
                    SessionFactory=new SecureSession(), 
                    IgnoreInvalidModels=ignoreInvalidModels,
                    SecurityHeaders=securityHeaders
                },
                CoreJSImport="VueJSMVCDotNet_core",
                VueImportPath= _VUE_IMPORT_PATH,
                VueLoaderImportPath= _VUE_LOADER_PATH,
                FileProvider=(blockFileProvider ? null : _fileProvider),
                MessageOptions= new MessageHandlerOptions()
                {
                    BaseURL="/resources/messages"
                },
                VueFilesOptions = new VueFilesHandlerOptions() { 
                    BaseURL = "/resources/vueFiles" 
                },
                LogWriter =  logWriter
            });
        }

        public static Engine CreateEngine(VueMiddleware middleware=null)
        {
            middleware = middleware?? CreateMiddleware(true);
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
            int status;
            engine.AddModule(
                "VueJSMVCDotNet_core",
                ReadJavascriptResponse(ExecuteRequest("GET", "/VueJSMVCDotNet_core.min.js", middleware,out status))
            );
            engine.SetValue("window", "{navigator:{language:'en-ca'}}");
            return engine;
        }

        public static string ReadJavascriptResponse(MemoryStream stream)
        {
            StreamReader sr = new StreamReader(stream);
            string content = sr.ReadToEnd();
            sr.Close();
            return content;
        }
    }
}
