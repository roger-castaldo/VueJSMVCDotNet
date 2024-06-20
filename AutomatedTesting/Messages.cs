using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VueJSMVCDotNet;

namespace AutomatedTesting
{
    [TestClass]
    public class Messages
    {
        [TestMethod()]
        public void EstablishMiddlewareWithoutFileProviderError()
        {
            Exception e = null;
            try
            {
                Utility.CreateMiddleware(true, true);
            }
            catch (Exception ex)
            {
                e=ex;
            }
            Assert.IsNotNull(e);
            Assert.IsInstanceOfType(e, typeof(ArgumentNullException));
            Assert.AreEqual("options", ((ArgumentNullException)e).ParamName);
        }

        [TestMethod]
        public void CachedPreviouslyGeneratedCode()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", middleware, out _)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string cachedContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", middleware, out _)).ReadToEnd();
            Assert.IsTrue(cachedContent.Length>0);
            Assert.AreEqual(content, cachedContent);
            middleware.Dispose();
        }

        [TestMethod]
        public void NotModifiedStatus()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            _=Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out _, out IHeaderDictionary responseHeaders);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));

            string content = new StreamReader(Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out int status, out responseHeaders, headers: new Dictionary<string, string>()
            {
                {"If-Modified-Since",DateTime.Today.AddDays(-1).ToUniversalTime().ToString("R") }
            })).ReadToEnd();

            Assert.AreEqual(0, content.Length);
            Assert.AreEqual(304, status);
            Assert.IsTrue(responseHeaders.ContainsKey("date"));
            Assert.IsTrue(responseHeaders.ContainsKey("etag"));
            Assert.AreEqual(DateTime.Today.AddDays(-1).ToUniversalTime().ToString("R"), responseHeaders["date"].ToString());
            middleware.Dispose();
        }

        [TestMethod]
        public void ModifiedStatus()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out _, out IHeaderDictionary responseHeaders);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));

            string content = new StreamReader(Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out int status, out responseHeaders, headers: new Dictionary<string, string>()
            {
                {"If-Modified-Since",DateTime.Today.ToUniversalTime().ToString("R") }
            })).ReadToEnd();

            Assert.AreNotEqual(0, content.Length);
            Assert.AreEqual(200, status);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));
            middleware.Dispose();
        }

        private void ExecuteTest(string additionalCode, string result,IMemoryCache cache=null)
        {
            using var middleware = Utility.CreateMiddleware(true,cache:cache);
            Engine eng = Utility.CreateEngine(middleware);
            try
            {
                eng.Execute(Constants.JAVASCRIPT_BASE);
                string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", middleware, out _)).ReadToEnd();
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @$"
    import {{Translate as translator}} from 'Translate';
    import {{SetLanguage}} from 'VueJSMVCDotNet_core';
{additionalCode}");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(result, ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            finally
            {
                eng.Dispose();
            }
        }

        [TestMethod()]
        public void TestMessageCallFileNotFound()
        {
            using var middleware = Utility.CreateMiddleware(true);
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/not_found.js", middleware, out int status)).ReadToEnd();
            Assert.AreEqual(404, status);
            Assert.AreEqual("Unable to locate requested file.", content);
            middleware.Dispose();
        }

        [TestMethod]
        public void TranslateNameWithDefaults()
        {
            ExecuteTest("export const name = translator('Name')", "Name");
        }

        [TestMethod]
        public void TranslateNameWithSpecificLanguage()
        {
            ExecuteTest(@"SetLanguage('fr');
export const name = translator('Name');", "Nome");
        }

        [TestMethod]
        public void TranslateFormattedWithInputs()
        {
            ExecuteTest("export const name = translator('Formatted',['hello world'])", String.Format("This is a formatted message {0} was the argument", new object[] { "hello world" }));
        }

        [TestMethod]
        public async Task FileChangeTriggers()
        {
            var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics=true
            });
            Utility.FileProvider.HidePath("AutomatedTesting.resources.messages.test.sp.json");
            ExecuteTest(@"SetLanguage('sp');
export const name = translator('Name');", "Name",cache);
            Utility.FileProvider.ShowPath("AutomatedTesting.resources.messages.test.sp.json");
            await Task.Delay(TimeSpan.FromSeconds(5));
            ExecuteTest(@"SetLanguage('sp');
export const name = translator('Name');", "Nombre",cache);
        }

        [TestMethod]
        public void MissingMessage()
        {
            ExecuteTest("export const name = translator('MissingMessage');", "MissingMessage");
        }
    }
}
