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
            Assert.IsTrue(content.Contains("const Icon = await import(`${hosturl.origin}/resources/vueFiles/icon.js`);"));
            Assert.IsFalse(content.Contains("const Button = await import(`${hosturl.origin}/resources/vueFiles/buttons/button.js`);"));
            Assert.IsTrue(content.Contains("${hosturl.origin}/resources/vueFiles/icon.js"));
        }

        [TestMethod]
        public async Task FileWithMultipleImportFormats()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/imports.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains("const {imp1} = await import(`${hosturl.origin}/imp1.js`);"));
            Assert.IsTrue(content.Contains("const {imp2, imp3} = await import(`${hosturl.origin}/components.js`);"));
            Assert.IsTrue(content.Contains("const {imp4} = await import(`${hosturl.origin}/resources/imps.js`);"));
            Assert.IsTrue(content.Contains("const {imp5} = await import(`${hosturl.origin}/resources/vueFiles/imps.js`);"));
            Assert.IsTrue(content.Contains("const {imp6} = await import(`${hosturl.origin}/imp6.js`);"));
            Assert.IsTrue(content.Contains("const {imp7, imp8} = await import(`${hosturl.origin}/components2.js`);"));
            Assert.IsTrue(content.Contains("const {imp9} = await import(`${hosturl.origin}/resources/imps9.js`);"));
            Assert.IsTrue(content.Contains("const {imp10} = await import(`${hosturl.origin}/resources/vueFiles/imps9.js`);"));
            Assert.IsTrue(content.Contains("const imp12 = await import(`${hosturl.origin}/resources/vueFiles/imp12.js`);"));
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
            //Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Unable to locate requested file.", content);
        }
    }
}
