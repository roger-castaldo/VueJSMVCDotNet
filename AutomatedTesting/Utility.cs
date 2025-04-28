using AutomatedTesting.Factory;
using AutomatedTesting.FileProvider;
using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    internal static class Utility
    {

        public static Task<(Stream responseStream,int responseStatus, HeaderCollection responseHeaders)> ExecuteRequestAsync(HttpMethod method, string path, WebApplicationFactory<Program> applicationFactory, object parameters = null, Dictionary<string, string> headers = null)
            => ExecuteRequestExportingHeaders(method, path, applicationFactory, parameters: parameters, headers: headers);

        public static Task<(Stream responseStream, int responseStatus, HeaderCollection responseHeaders)> ExecuteRequestAsync(HttpMethod method, string path, WebApplicationFactory<Program> applicationFactory, Dictionary<string, StringValues> formData, Dictionary<string, string> headers = null)
            => ExecuteRequestExportingHeaders(method, path, applicationFactory, formData: formData, headers: headers);
        public static async Task<(Stream responseStream, int responseStatus, HeaderCollection responseHeaders)> ExecuteRequestExportingHeaders(HttpMethod method, string path, WebApplicationFactory<Program> applicationFactory, object parameters = null, Dictionary<string, string> headers = null, Dictionary<string, StringValues> formData = null)
        {
            var client = applicationFactory.CreateClient();
            var request = new HttpRequestMessage(method,path);

            if (parameters != null)
                request.Content = JsonContent.Create(parameters);
            else if (formData!=null)
            {
                var content = new MultipartFormDataContent();
                foreach(var pair in formData)
                {
                    foreach(var val in pair.Value)
                        content.Add(new StringContent(val, Encoding.UTF8, MediaTypeNames.Text.Plain), pair.Key);
                }   
                request.Content = content;
            }

            if (headers != null)
            {
                foreach (string key in headers.Keys)
                    request.Headers.Add(key, headers[key]);
            }

            var response = await client.SendAsync(request);

            return (await response.Content.ReadAsStreamAsync(), (int)response.StatusCode, new(response.Headers,response.Content.Headers));
        }

        internal static string ReadResponse(Stream ms)
        {
            string content = new StreamReader(ms).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            return content;
        }

        public static object ReadJSONResponse(Stream ms)
            => JSON.JsonDecode(ReadResponse(ms));

        private const string _VUE_IMPORT_PATH = "vue";
        private const string _VUE_LOADER_PATH = "vue-loader";

        public static (WebApplicationFactory<Program> webApplicationFactory, IDataStore dataStore, SecureSession secureSession) CreateApplication(bool ignoreInvalidModels, ILogger logWriter = null, IMemoryCache cache = null)
        {
            IDataStore dataStore = new DataStore();
            var (webApplicationFactory, secureSession)= CreateApplication(ignoreInvalidModels, dataStore, logWriter:logWriter, cache:cache);
            return (webApplicationFactory, dataStore, secureSession);
        }

        public static (WebApplicationFactory<Program> webApplicationFactory, SecureSession secureSession) CreateApplication(bool ignoreInvalidModels, IDataStore store, ILogger logWriter = null, IMemoryCache cache = null)
        {
            var secureSession = new SecureSession();
            return (CreateApplication(ignoreInvalidModels, store, secureSession, logWriter: logWriter, cache: cache), secureSession);
        }

        public static (WebApplicationFactory<Program> webApplicationFactory, IDataStore dataStore) CreateApplication(bool ignoreInvalidModels, SecureSession secureSession, ILogger logWriter = null, IMemoryCache cache = null)
        {
            IDataStore dataStore = new DataStore();
            return (CreateApplication(ignoreInvalidModels, dataStore, secureSession, logWriter: logWriter, cache: cache), dataStore);
        }

        public static WebApplicationFactory<Program> CreateApplication(bool ignoreInvalidModels, IDataStore store, SecureSession secureSession, ILogger logWriter = null, IMemoryCache cache = null)
            => new TestApplicationFactory(ignoreInvalidModels,logWriter,cache,store,secureSession);

        public static async Task<Engine> CreateEngineAsync(WebApplicationFactory<Program> applicationFactory = null)
        {
            applicationFactory ??= CreateApplication(true).webApplicationFactory;
            var opt = new Options();
            opt.EnableModules(typeof(Utility).Assembly.Location[..typeof(Utility).Assembly.Location.LastIndexOf(Path.DirectorySeparatorChar)]);
            var engine = new Engine(opt);
            var sr = new StreamReader(new FileStream("./resources/vue.esm-browser.prod.js", FileMode.Open, FileAccess.Read, FileShare.Read));
            engine.Modules.Add(
                _VUE_IMPORT_PATH,
                await sr.ReadToEndAsync()
            );
            sr.Close();
            sr = new StreamReader(new FileStream("./resources/vue3-sfc-loader.esm.js", FileMode.Open, FileAccess.Read, FileShare.Read));
            engine.Modules.Add(
                _VUE_LOADER_PATH,
                await sr.ReadToEndAsync()
            );
            sr.Close();
            engine.Modules.Add(
                "VueJSMVCDotNet_core",
                ReadJavascriptResponse((await ExecuteRequestAsync(HttpMethod.Get, "/VueJSMVCDotNet_core.min.js", applicationFactory)).responseStream)
            );
            return engine
                .SetValue("window", "{navigator:{language:'en-ca'}}")
                .Execute("const document = {currentScript:{src:'http://localhost/'},createElement:function(elem){return {tag:elem};}}")
                .Execute(@"class URL {
  constructor(url) {
    this.origin = url??'http://localhost/';
  }
}");
        }

        public static string ReadJavascriptResponse(Stream stream)
        {
            var sr = new StreamReader(stream);
            string content = sr.ReadToEnd();
            sr.Close();
            return content;
        }
    }
}
