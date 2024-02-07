using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System.IO;

namespace AutomatedTesting
{
    [TestClass]
    public class VueFiles
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

        [TestMethod]
        public void FolderWithFiles()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/buttons.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains("import Icon from '/resources/vuefiles/icon.vue';"));
            Assert.IsTrue(content.Contains("import Button from '/resources/vuefiles/buttons/button.vue';"));
            Assert.IsTrue(content.Contains("/resources/vuefiles/icon.js"));
            Assert.IsFalse(content.Contains("/resources/vuefiles/buttons/button.js"));
        }

        [TestMethod()]
        public void TestMessageCallFileNotFound()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/vueFiles/not_found.js", _middleware, out status)).ReadToEnd();
            Assert.AreEqual(404, status);
            Assert.AreEqual("Unable to locate requested file.", content);
        }
    }
}
