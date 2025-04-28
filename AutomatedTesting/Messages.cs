using AutomatedTesting.FileProvider;
using Jint;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class Messages
    {
        [TestMethod]
        public async Task CachedPreviouslyGeneratedCode()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true,cache:cache);
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));

            //Act
            Assert.IsTrue(content.Length > 0);
            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            var cachedContent = await new StreamReader(responseStream).ReadToEndAsync();
            
            //Assert
            Assert.IsTrue(cachedContent.Length>0);
            Assert.AreEqual(content, cachedContent);
        }

        [TestMethod]
        public async Task NotModifiedStatus()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);

            //Act
            var (_, _, responseHeaders) = await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            Assert.IsTrue(responseHeaders.TryGetValues(HeaderNames.CacheControl,out _));
            await Task.Delay(TimeSpan.FromSeconds(10));

            var (responseStream, responseStatus, secondResponseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get,"/resources/messages/test.js", webApplicationFactory,
                headers: new Dictionary<string, string>()
            {
                {HeaderNames.IfModifiedSince,DateTime.Today.AddDays(-2).ToUniversalTime().ToString("R") }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();
                
            //Assert
            Assert.AreEqual(0, content.Length);
            Assert.AreEqual(304, responseStatus);
            Assert.IsTrue(secondResponseHeaders.TryGetValues("date", out _));
            Assert.IsTrue(secondResponseHeaders.TryGetValues("etag", out _));
            Assert.AreEqual(DateTime.Today.AddDays(-1).ToUniversalTime().ToString("R"), secondResponseHeaders.GetValues("date").First());
        }

        [TestMethod]
        public async Task ModifiedStatus()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);

            //Act
            var (_, _, responseHeaders) = await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            Assert.IsTrue(responseHeaders.TryGetValues("Cache-Control", out _));

            var (responseStream, responseStatus, secondResponseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory,
                headers: new Dictionary<string, string>()
            {
                {HeaderNames.IfModifiedSince,DateTime.Today.ToUniversalTime().ToString("R") }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreNotEqual(0, content.Length);
            Assert.AreEqual(200, responseStatus);
            Assert.IsTrue(secondResponseHeaders.TryGetValues("Cache-Control", out _));
        }

        [TestMethod()]
        public async Task TestMessageCallFileNotFound()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get,"/resources/messages/not_found.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Unable to locate requested file.", content);
        }

        private async Task ExecuteTestAsync(string additionalCode, string result, IMemoryCache cache = null, WebApplicationFactory<Program> webApplicationFactory = null)
        {
            if (webApplicationFactory==null)
                (webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);
            using Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Execute(Constants.JAVASCRIPT_BASE);
                var (responseStream, _, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
                var content = await new StreamReader(responseStream).ReadToEndAsync();
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @$"
    import {{Translate as translator}} from 'Translate';
    import {{SetLanguage}} from 'VueJSMVCDotNet_core';
{additionalCode}");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(result, ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public async Task TranslateNameWithDefaults()
        {
            await ExecuteTestAsync("export const name = translator('Name')", "Name");
        }

        [TestMethod]
        public async Task TranslateNameWithSpecificLanguage()
        {
            await ExecuteTestAsync(@"SetLanguage('fr');
export const name = translator('Name');", "Nome");
        }

        [TestMethod]
        public async Task TranslateFormattedWithInputs()
        {
            await ExecuteTestAsync("export const name = translator('Formatted',['hello world'])", String.Format("This is a formatted message {0} was the argument", new object[] { "hello world" }));
        }

        [TestMethod]
        public async Task FileChangeTriggers()
        {
            var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics=true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);
            var fileProvider = (EmbeddedResourceFileProvider)webApplicationFactory.Services.GetRequiredService<IFileProvider>();
            fileProvider.HidePath("AutomatedTesting.resources.messages.test.sp.json");
            
            await ExecuteTestAsync(@"SetLanguage('sp');
export const name = translator('Name');", "Name", webApplicationFactory:webApplicationFactory);
            fileProvider.ShowPath("AutomatedTesting.resources.messages.test.sp.json");
            
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            await ExecuteTestAsync(@"SetLanguage('sp');
export const name = translator('Name');", "Nombre", cache, webApplicationFactory: webApplicationFactory);
        }

        [TestMethod]
        public async Task MissingMessage()
        {
            await ExecuteTestAsync("export const name = translator('MissingMessage');", "MissingMessage");
        }
    }
}
