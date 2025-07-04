using AutomatedTesting.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class JsonDataTypeTests
    {
        [TestMethod]
        public async Task TestIPAddressV4()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var input = IPAddress.Broadcast;

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckIPAddress", webApplicationFactory,
            parameters: new Hashtable() {
                { "input", input.ToString() }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType<string>(content);
            Assert.AreEqual(content, input.ToString());
        }

        [TestMethod]
        public async Task TestIPAddressV6()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var input = IPAddress.IPv6Loopback;

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckIPAddress", webApplicationFactory,
            parameters: new Hashtable() {
                { "input", input.ToString() }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType<string>(content);
            Assert.AreEqual(content, input.ToString());
        }

        [TestMethod]
        public async Task TestGuid()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var input = Guid.NewGuid();

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckGuid", webApplicationFactory,
            parameters: new Hashtable() {
                { "input", input.ToString() }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(content);
            Assert.IsInstanceOfType<Guid>(content);
            Assert.AreEqual(content, input);
        }

        [TestMethod]
        public async Task TestModelConvertFromID()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var input = mPersonHandler.Persons.First().id;

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckModel", webApplicationFactory,
            parameters: new Hashtable() {
                { "person", input }
            });
            var content = Utility.ReadResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(content);
            Assert.AreEqual(content, input);
        }

        [TestMethod]
        public async Task TestModelConvertAsNull()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var input = "not valid";

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckNullModelAsync", webApplicationFactory,
            parameters: new Hashtable() {
                { "id", input }
            });
            var content = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNull(content);
        }

        [TestMethod]
        public async Task TestExceptionThrownFromMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckExceptionAsync", webApplicationFactory,
            parameters: new Hashtable() {
                { "input", false }
            });
            var (errorResponseStream, errorResponseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.JsonDataTypesModelRoute}/CheckExceptionAsync", webApplicationFactory,
            parameters: new Hashtable() {
                { "input", true }
            });

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse((bool)Utility.ReadJSONResponse(responseStream));
            Assert.AreEqual(500, errorResponseStatus);
            Assert.AreEqual("Internal Server Error", Utility.ReadResponse(errorResponseStream));
        }
    }
}
