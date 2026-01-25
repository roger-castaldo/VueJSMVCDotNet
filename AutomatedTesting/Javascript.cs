using Jint;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class Javascript
    {
        [TestMethod]
        public async Task CoreJavascriptValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"/{Constants.CORE_IMPORT_PATH}.min.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("core", content);
                eng.Modules.Add("custom", @"import {isEqual} from 'core';
export const testResult = isEqual('test','test');");
                var ns = eng.Modules.Import("custom");
                Assert.IsTrue(ns.Get("testResult").AsBoolean());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("mPerson", content);
                eng.Modules.Add("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptCompressedGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.min.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("mPerson", content);
                eng.Modules.Add("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptCompressionPerformance()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);
            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.min.js", webApplicationFactory);
            var minContent = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);

            //Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(minContent));
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public async Task JavascriptGenerationWithLinkedTypesValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.LocationModelRoute}.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            content=content.Replace("'/models/mperson.js'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);
            content=content.Replace("'/models/mgroup.js'", "'mgroup'", StringComparison.InvariantCultureIgnoreCase);

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);
            var personContent = await new StreamReader(responseStream).ReadToEndAsync();

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.GroupModelRoute}.js", webApplicationFactory);
            var groupContent = await new StreamReader(responseStream).ReadToEndAsync();
            groupContent=groupContent.Replace($"'{Constants.PersonModelRoute}.js'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("mperson", personContent);
                eng.Modules.Add("mgroup", groupContent);
                eng.Modules.Add("mLocation", content);
                eng.Modules.Add("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptGenerationWithLinkedTypesValidationAsModule()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.LocationModelRoute}.mjs", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            content=content.Replace("'/models/mperson.mjs'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);
            content=content.Replace("'/models/mgroup.mjs'", "'mgroup'", StringComparison.InvariantCultureIgnoreCase);

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.mjs", webApplicationFactory);
            var personContent = await new StreamReader(responseStream).ReadToEndAsync();

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.GroupModelRoute}.mjs", webApplicationFactory);
            var groupContent = await new StreamReader(responseStream).ReadToEndAsync();
            groupContent=groupContent.Replace($"'{Constants.PersonModelRoute}.mjs'", "'mperson'", StringComparison.InvariantCultureIgnoreCase);

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("mperson", personContent);
                eng.Modules.Add("mgroup", groupContent);
                eng.Modules.Add("mLocation", content);
                eng.Modules.Add("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptCompressedGenerationWithLinkedTypesValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.LocationModelRoute}.min.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            content=content.Replace($"\"{Constants.PersonModelRoute}.min.js\"", "\"mperson\"", StringComparison.InvariantCultureIgnoreCase);
            content=content.Replace($"\"{Constants.GroupModelRoute}.min.js\"", "\"mgroup\"", StringComparison.InvariantCultureIgnoreCase);

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);
            var personContent = await new StreamReader(responseStream).ReadToEndAsync();

            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.GroupModelRoute}.js", webApplicationFactory);
            var groupContent = await new StreamReader(responseStream).ReadToEndAsync();
            groupContent=groupContent.Replace($"'{Constants.PersonModelRoute}.js'", "\"mperson\"", StringComparison.InvariantCultureIgnoreCase);

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("mperson", personContent);
                eng.Modules.Add("mgroup", groupContent);
                eng.Modules.Add("mLocation", content);
                eng.Modules.Add("custom", @"import { mLocation } from 'mLocation';
export const name = 'John';");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task JavascriptCacheControlHeaders()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);

            //Act
            var (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(headers.CacheControl?.ToString()));
            Assert.IsTrue(headers.GetValues(HeaderNames.LastModified).Any());

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = string.Join(';', headers.GetValues(HeaderNames.LastModified));

            (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory,
            headers: new Dictionary<string, string>()
            {
                { HeaderNames.IfModifiedSince,lastModified}
            });

            Assert.AreEqual(304, responseStatus);
            Assert.AreEqual(0, responseStream.Length);
        }

        [TestMethod]
        public async Task MessageScriptGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @"import {Translate} from 'Translate';
export const name = Translate('Name',null);");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("Name", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task MessageScriptCompressedGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.min.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("Translate", content);
                eng.Modules.Add("custom", @"import {Translate} from 'Translate';
export const name = Translate('Name',null,'en');");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual("Name", ns.Get("name").AsString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task MessageScriptCompressionPerformance()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);
            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.min.js", webApplicationFactory);
            var minContent = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);

            //Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(minContent));
            Assert.IsTrue(minContent.Length < content.Length);
        }


        [TestMethod]
        public async Task MessageScriptCacheControlHeaders()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);

            //Act
            var (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(headers.CacheControl?.ToString()));
            Assert.IsTrue(headers.GetValues(HeaderNames.LastModified).Any());

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = string.Join(';', headers.GetValues(HeaderNames.LastModified));

            (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/messages/test.js", webApplicationFactory,
            headers: new Dictionary<string, string>()
            {
                { HeaderNames.IfModifiedSince,lastModified}
            });

            Assert.AreEqual(304, responseStatus);
            Assert.AreEqual(0, responseStream.Length);
        }

        [TestMethod]
        public async Task VueFileScriptGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("notification", content);
                eng.Modules.Add("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.__name!==undefined;");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(true, ns.Get("check").AsBoolean());
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
        public async Task VueFileScriptCompressedGenerationValidation()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var start = Stopwatch.GetTimestamp();

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.min.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            System.Diagnostics.Trace.WriteLine($"Total time to generate: {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms of size {content.Length}b");
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));

            Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Modules.Add("notification", content);
                eng.Modules.Add("custom", @"import notification from 'notification';
export const check = notification!==undefined && notification!==null && notification.__name!==undefined;");
                var ns = eng.Modules.Import("custom");
                Assert.AreEqual(true, ns.Get("check").AsBoolean());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task VueFileScriptCompressionPerformance()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);
            (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.min.js", webApplicationFactory);
            var minContent = await new StreamReader(responseStream).ReadToEndAsync();
            Assert.AreEqual(200, responseStatus);

            //Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(minContent));
            Assert.IsTrue(minContent.Length < content.Length);
        }

        [TestMethod]
        public async Task VueFileScriptCacheControlHeaders()
        {
            //Arrange
            using var cache = new MemoryCache(new MemoryCacheOptions()
            {
                TrackStatistics = true
            });
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, cache: cache);

            //Act
            var (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.IsFalse(string.IsNullOrWhiteSpace(headers.CacheControl?.ToString()));
            Assert.IsTrue(headers.GetValues(HeaderNames.LastModified).Any());

            Assert.AreEqual(1, cache.GetCurrentStatistics().CurrentEntryCount);

            string lastModified = string.Join(';', headers.GetValues(HeaderNames.LastModified));

            (responseStream, responseStatus, headers)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/vueFiles/notification.js", webApplicationFactory,
            headers: new Dictionary<string, string>()
            {
                { HeaderNames.IfModifiedSince,lastModified}
            });

            Assert.AreEqual(304, responseStatus);
            Assert.AreEqual(0, responseStream.Length);
        }
    }
}
