using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class Javascript
    {
        [TestMethod]
        public void CoreJavascriptValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            string content = Utility.ReadResponse(Utility.ExecuteRequest("GET", "/VueJSMVCDotNet_core.min.js", middleware, out status));

            Assert.AreEqual(200, status);
            Assert.IsTrue(content.Length>0);

            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("core", content);
                eng.Modules.Add("custom", @"import {isEqual} from 'core';
export const testResult = isEqual('test','test');");
                var ns = eng.Modules.Import("custom");
                Assert.IsTrue(ns.Get("testResult").AsBoolean());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var watch = new Stopwatch();
            watch.Start();
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status));
            watch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {watch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("mPerson", content);
                eng.Modules.Add("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptGenerationWithSecurityHeadersValidation()
        {
            using var middleware = Utility.CreateMiddleware(true, securityHeaders: new string[] { "sechead1", "sec_head_2" });
            int status;
            var watch = new Stopwatch();
            watch.Start();
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status));
            watch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {watch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine(middleware: middleware);
            try
            {
                eng.Modules.Add("mPerson", content);
                eng.Modules.Add("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptCompressedGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.min.js", middleware, out status));
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("mPerson", content);
                eng.Modules.Add("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptCompressionPerformance()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.min.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public void JavascriptGenerationWithLinkedTypesValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mLocation.js", middleware, out status));
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            content=content.Replace("'/resources/scripts/mperson.js'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("mperson", Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status)));
                eng.Modules.Add("mLocation", content);
                eng.Modules.Add("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptCompressedGenerationWithLinkedTypesValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mLocation.min.js", middleware, out status));
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            content=content.Replace("'/resources/scripts/mperson.js'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("mperson", Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status)));
                eng.Modules.Add("mLocation", content);
                eng.Modules.Add("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptCacheControlHeaders()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/scripts/mPerson.js", middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", middleware, out status, headers: new Dictionary<string, string>()
            {
                { "If-Modified-Since",lastModified}
            });

            Assert.AreEqual(304, status);
            Assert.AreEqual(0, stream.Length);
        }

        [TestMethod]
        public void MessageScriptGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", middleware, out status)).ReadToEnd();
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @"import {Translate} from 'Translate';
export const name = Translate('Name',null);");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("Name", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void MessageScriptCompressedGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.min.js", middleware, out status)).ReadToEnd();
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @"import {Translate} from 'Translate';
export const name = Translate('Name',null,'en');");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("Name", ns.Get("name").AsString());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void MessageScriptCompressionPerformance()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.min.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }


        [TestMethod]
        public void MessageScriptCacheControlHeaders()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", middleware, out status, out headers, headers: new Dictionary<string, string>()
            {
                { "If-Modified-Since",lastModified}
            });

            Assert.AreEqual(304, status);
            Assert.AreEqual(0, stream.Length);
            Assert.IsTrue(headers.ContainsKey("accept-ranges"));
            Assert.IsTrue(headers.ContainsKey("date"));
            Assert.IsTrue(headers.ContainsKey("etag"));
        }

        [TestMethod]
        public void VueFileScriptGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.js", middleware, out status)).ReadToEnd();
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("notification", content);
                eng.Modules.Add("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.name!==undefined;");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(true, ns.Get("check").AsBoolean());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message+"\r\n"+
                e.StackTrace+"\r\n"+
                content);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void VueFileScriptCompressedGenerationValidation()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.min.js", middleware, out status)).ReadToEnd();
            stopwatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {stopwatch.ElapsedMilliseconds}ms of size {content.Length}b");
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Modules.Add("notification", content);
                eng.Modules.Add("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.name!==undefined;");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(true, ns.Get("check").AsBoolean());
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void VueFileScriptCompressionPerformance()
        {
            using var middleware = Utility.CreateMiddleware(true);
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.min.js", middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public void VueFileScriptCacheControlHeaders()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            using var middleware = Utility.CreateMiddleware(true, cache: cache);
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/vueFiles/notification.js", middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/vueFiles/notification.js", middleware, out status, out headers, headers: new Dictionary<string, string>()
            {
                { "If-Modified-Since",lastModified}
            });

            Assert.AreEqual(304, status);
            Assert.AreEqual(0, stream.Length);
            Assert.IsTrue(headers.ContainsKey("accept-ranges"));
            Assert.IsTrue(headers.ContainsKey("date"));
            Assert.IsTrue(headers.ContainsKey("etag"));
        }
    }
}
