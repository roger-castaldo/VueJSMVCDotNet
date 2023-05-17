using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class Javascript
    {
        private VueMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware = Utility.CreateMiddleware(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        private delegate object _delCreateWeakMap();

        [TestMethod]
        public void CoreJavascriptValidation()
        {
            int status;
            string content = Utility.ReadResponse(Utility.ExecuteRequest("GET", "/VueJSMVCDotNet_core.min.js", _middleware, out status));

            Assert.AreEqual(200, status);
            Assert.IsTrue(content.Length>0);

            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("core", content);
                eng.AddModule("custom", @"import {isEqual} from 'core';
export const testResult = isEqual('test','test');");
                var ns = eng.ImportModule("custom");
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
            int status;
            DateTime now = DateTime.Now;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.js", _middleware, out status));
            System.Diagnostics.Debug.WriteLine("Total time to generate: {0}ms of size {1}b", new object[]
            {
                DateTime.Now.Subtract(now).TotalMilliseconds,
                System.Text.ASCIIEncoding.ASCII.GetBytes(content).Length
            });
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mPerson", content);
                eng.AddModule("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch(Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptGenerationWithSecurityHeadersValidation()
        {
            _middleware = Utility.CreateMiddleware(true,securityHeaders:new string[] {"sechead1","sec_head_2"});
            int status;
            DateTime now = DateTime.Now;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status));
            System.Diagnostics.Debug.WriteLine("Total time to generate: {0}ms of size {1}b", new object[]
            {
                DateTime.Now.Subtract(now).TotalMilliseconds,
                System.Text.ASCIIEncoding.ASCII.GetBytes(content).Length
            });
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine(middleware:_middleware);
            try
            {
                eng.AddModule("mPerson", content);
                eng.AddModule("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.min.js", _middleware, out status));
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mPerson", content);
                eng.AddModule("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public void JavascriptGenerationWithLinkedTypesValidation()
        {
            int status;
            DateTime now = DateTime.Now;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mLocation.js", _middleware, out status));
            System.Diagnostics.Debug.WriteLine("Total time to generate: {0}ms of size {1}b", new object[]
            {
                DateTime.Now.Subtract(now).TotalMilliseconds,
                System.Text.ASCIIEncoding.ASCII.GetBytes(content).Length
            });
            Assert.IsTrue(content.Length > 0);
            content=content.Replace("'/resources/scripts/mperson.js'", "'mperson'");
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mperson", Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status)));
                eng.AddModule("mLocation", content);
                eng.AddModule("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
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
            int status;
            DateTime now = DateTime.Now;
            string content = Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mLocation.min.js", _middleware, out status));
            System.Diagnostics.Debug.WriteLine("Total time to generate: {0}ms of size {1}b", new object[]
            {
                DateTime.Now.Subtract(now).TotalMilliseconds,
                System.Text.ASCIIEncoding.ASCII.GetBytes(content).Length
            });
            Assert.IsTrue(content.Length > 0);
            content=content.Replace("'/resources/scripts/mperson.js'", "'mperson'");
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mperson", Utility.ReadJavascriptResponse(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status)));
                eng.AddModule("mLocation", content);
                eng.AddModule("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
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
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/scripts/mPerson.js", _middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status, headers: new Dictionary<string, string>()
            {
                { "If-Modified-Since",lastModified}
            });

            Assert.AreEqual(304, status);
            Assert.AreEqual(0,stream.Length);
        }

        [TestMethod]
        public void JavascriptInternalCache()
        {
            int status;
            var stream = Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);

            var length = stream.Length;

            stream = Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status);
            Assert.AreEqual(200, status);
            Assert.AreEqual(length, stream.Length);
        }

        [TestMethod]
        public void MessageScriptGenerationValidation()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("Translate", content);
                eng.AddModule("custom", @"import translator from 'Translate';
export const name = translator('Name',null,'en');");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("Translate", content);
                eng.AddModule("custom", @"import translator from 'Translate';
export const name = translator('Name',null,'en');");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }


        [TestMethod]
        public void MessageScriptCacheControlHeaders()
        {
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status,out headers, headers: new Dictionary<string, string>()
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("notification", content);
                eng.AddModule("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.name!==undefined;");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("notification", content);
                eng.AddModule("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.name!==undefined;");
                var ns = eng.ImportModule("custom");
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
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/notification.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public void VueFileScriptCacheControlHeaders()
        {
            int status;
            IHeaderDictionary headers;
            var stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/vueFiles/notification.js", _middleware, out status, out headers);
            Assert.AreEqual(200, status);
            Assert.IsTrue(stream.Length>0);
            Assert.IsTrue(headers.ContainsKey("Cache-Control"));
            Assert.IsTrue(headers.ContainsKey("Last-Modified"));

            string lastModified = headers["Last-Modified"].ToString();

            stream = Utility.ExecuteRequestExportingHeaders("GET", "/resources/vueFiles/notification.js", _middleware, out status, out headers, headers: new Dictionary<string, string>()
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
