using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace AutomatedTesting
{
    [TestClass]
    public class Messages
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
            Assert.AreEqual("fileProvider", ((ArgumentNullException)e).ParamName);
        }

        [TestMethod]
        public void JavascriptGenerationValidation()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("Translate", content);
                eng.AddModule("custom", @"import translator from 'Translate';
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
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("Translate", content);
                eng.AddModule("custom", @"import translator from 'Translate';
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
        public void CachedPreviouslyGeneratedCode()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string cachedContent = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(cachedContent.Length>0);
            Assert.AreEqual(content, cachedContent);
        }

        [TestMethod]
        public void NotModifiedStatus()
        {
            int status;
            IHeaderDictionary responseHeaders;
            Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status,out responseHeaders);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));

            string content = new StreamReader(Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status, out responseHeaders,headers:new Dictionary<string, string>()
            {
                {"If-Modified-Since",DateTime.Today.AddDays(-1).ToUniversalTime().ToString("R") }
            })).ReadToEnd();

            Assert.AreEqual(0, content.Length);
            Assert.AreEqual(304, status);
            Assert.IsTrue(responseHeaders.ContainsKey("date"));
            Assert.IsTrue(responseHeaders.ContainsKey("etag"));
            Assert.AreEqual(DateTime.Today.AddDays(-1).ToUniversalTime().ToString("R"), responseHeaders["date"].ToString());
        }

        [TestMethod]
        public void ModifiedStatus()
        {
            int status;
            IHeaderDictionary responseHeaders;
            Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status, out responseHeaders);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));

            string content = new StreamReader(Utility.ExecuteRequestExportingHeaders("GET", "/resources/messages/test.js", _middleware, out status, out responseHeaders, headers: new Dictionary<string, string>()
            {
                {"If-Modified-Since",DateTime.Today.ToUniversalTime().ToString("R") }
            })).ReadToEnd();

            Assert.AreNotEqual(0, content.Length);
            Assert.AreEqual(200, status);
            Assert.IsTrue(responseHeaders.ContainsKey("Cache-Control"));
        }

        private void _ExecuteTest(string additionalCode, string result)
        {
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Execute(Constants.JAVASCRIPT_BASE);
                int status;
                string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/test.js", _middleware, out status)).ReadToEnd();
                eng.AddModule("Translate", content);
                eng.AddModule("custom", string.Format(@"
    import translator from 'Translate';
export const name = "+additionalCode));
                var ns = eng.ImportModule("custom");
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
            Engine eng = Utility.CreateEngine();
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/messages/not_found.js", _middleware, out status)).ReadToEnd();
            Assert.AreEqual(404, status);
            Assert.AreEqual("Unable to locate requested file.", content);
        }

        [TestMethod]
        public void TranslateNameWithDefaults()
        {
            _ExecuteTest("translator('Name')", "Name");
        }

        [TestMethod]
        public void TranslateNameWithSpecificLanguage()
        {
            _ExecuteTest("translator('Name',null,'fr')", "Nome");
        }

        [TestMethod]
        public void TranslateFormattedWithInputs()
        {
            _ExecuteTest("translator('Formatted',['hello world'])", String.Format("This is a formatted message {0} was the argument",new object[] {"hello world"}));
        }

        [TestMethod]
        public void FileChangeTriggers()
        {
            Utility.FileProvider.HidePath("AutomatedTesting.resources.messages.test.fr.json");
            _ExecuteTest("translator('Name',null,'fr')", "Name");
            Utility.FileProvider.ShowPath("AutomatedTesting.resources.messages.test.fr.json");
            _ExecuteTest("translator('Name',null,'fr')", "Nome");
        }

        [TestMethod]
        public void MissingMessage()
        {
            _ExecuteTest("translator('MissingMessage')", "MissingMessage");
        }
    }
}
