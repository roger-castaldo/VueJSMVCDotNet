using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class VueFiles
    {
        [TestMethod]
        public async Task FolderWithFiles()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/buttons.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains("import Icon from '${hosturl.origin}/resources/vueFiles/icon.vue';"));
            Assert.IsTrue(content.Contains("import Button from '${hosturl.origin}/resources/vueFiles/buttons/button.vue';"));
            Assert.IsTrue(content.Contains("${hosturl.origin}/resources/vueFiles/icon.js"));
            Assert.IsFalse(content.Contains("/resources/vueFiles/buttons/button.js"));
        }

        [TestMethod()]
        public async Task TestMessageCallFileNotFound()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/not_found.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Unable to locate requested file.", content);
        }

        [TestMethod]
        public async Task VueFileLinkedToModelHandlers()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/person.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains("import  { mPerson } from  '${hosturl.origin}/models/mPerson.js';"));
            Assert.IsTrue(content.Contains("await import('${hosturl.origin}/models/mGroup.mjs');"));
        }
    }
}
