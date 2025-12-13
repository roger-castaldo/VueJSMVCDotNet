using AutomatedTesting.Handlers;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class StaticMethodCall
    {
        [TestMethod]
        public async Task TestStaticMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string firstName = "Testing123";
            string lastName = "Testing1234";

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/FormatName", webApplicationFactory,
            parameters: new Hashtable() {
                { "firstName", firstName },
                { "lastName", lastName }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual(mPersonHandler.FormatName(lastName, firstName), content);
        }

        [TestMethod]
        public async Task TestStaticMethodWithFormData()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string firstName = "Testing123";
            string lastName = "Testing1234";

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/FormatName", webApplicationFactory,
            formData: new Dictionary<string, StringValues>()
            {
                {"firstName",firstName },
                {"lastName",lastName }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual(mPersonHandler.FormatName(lastName, firstName), content);
        }

        [TestMethod]
        public async Task TestStaticMethodWithFormDataArrayValues()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/FormatNames", webApplicationFactory,
            formData: new Dictionary<string, StringValues>()
            {
                {"firstName",new(firstName) },
                {"lastName",new(lastName) }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(content, typeof(ArrayList));
            var al = (ArrayList)content;
            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[1], firstName[1]), al[1]);
        }

        [TestMethod]
        public async Task TestStaticMethodWithFormDataJsonEncoded()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/FormatNames", webApplicationFactory,
            formData: new Dictionary<string, StringValues>()
            {
                {"firstName:json",JSON.JsonEncode(firstName) },
                {"lastName:json",JSON.JsonEncode(lastName) }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(content, typeof(ArrayList));
            var al = (ArrayList)content;
            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[1], firstName[1]), al[1]);
        }

        [TestMethod]
        public async Task TestStaticMethodWithFormDataJsonEncodedMultipleValues()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/FormatNames", webApplicationFactory,
            formData: new Dictionary<string, StringValues>()
            {
                {"firstName:json",new string[]{ JSON.JsonEncode(firstName[0]), JSON.JsonEncode(firstName[1]) } },
                {"lastName:json",new string[]{ JSON.JsonEncode(lastName[0]), JSON.JsonEncode(lastName[1]) } }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(content, typeof(ArrayList));
            var al = (ArrayList)content;
            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPersonHandler.FormatName(lastName[1], firstName[1]), al[1]);
        }

        [TestMethod]
        public async Task TestStaticMethodNotFound()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string firstName = "Testing123";
            string lastName = "Testing1234";

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/FormatName", webApplicationFactory,
            parameters: new Hashtable() {
                { "firstName", firstName },
                { "lastName", lastName },
                {"userName","testing" }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Unable to locate method with matching parameters", content);
        }

        [TestMethod]
        public async Task TestStaticMethodWithNullNotNullArguement()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            string firstName = null;
            string lastName = "Testing1234";

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/FormatName", webApplicationFactory,
            parameters: new Hashtable() {
                { "firstName", firstName },
                { "lastName", lastName }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Unable to locate method with matching parameters", content);
        }

        [TestMethod]
        public async Task TestStaticMethodWithObjectResult()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/ProduceObject", webApplicationFactory,
            parameters: new Hashtable() {
                {"isnull",false }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
        }

        [TestMethod]
        public async Task TestStaticMethodWithNullResult()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/ProduceObject", webApplicationFactory,
            parameters: new Hashtable() {
                {"isnull",true }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestStaticVoidMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/VoidMethodCall", webApplicationFactory,
            parameters: new Hashtable() {
                {"parameter","" }
            });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsTrue(content.Length==0);
        }

        [TestMethod]
        public async Task TestMethodWithSpecialParameterInjections()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/CheckSpecialItems", webApplicationFactory,
            parameters: new Hashtable() { });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual(content, "true");
        }
    }
}


