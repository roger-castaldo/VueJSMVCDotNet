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
    }
}
