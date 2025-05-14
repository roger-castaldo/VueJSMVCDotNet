using AutomatedTesting.Handlers;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class SecurityTests
    {
        public const string _NOT_ALLOWED_MESSAGE = "Not Authorized";
        public const int _NOT_ALLOWED_STATUS = 403;
        
        private async Task ExecuteTest(HttpMethod method,string url, string[] security, bool shouldSucceeed = false, Hashtable parameters = null)
        {
            //Act
            (var webApplicationFactory, _) = Utility.CreateApplication(true,new SecureSession(security));

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(method, url, webApplicationFactory,
                parameters: parameters);
            var result = Utility.ReadResponse(responseStream);

            //Assert
            if (!shouldSucceeed)
            {
                Assert.AreEqual(_NOT_ALLOWED_MESSAGE, result);
                Assert.AreEqual(_NOT_ALLOWED_STATUS, responseStatus);
            }
            else
            {
                Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, result);
                Assert.AreNotEqual(_NOT_ALLOWED_STATUS, responseStatus);
            }
            await webApplicationFactory.DisposeAsync();
        }

        [TestMethod]
        public async Task TestLoadSecurity()
        {
            await ExecuteTest(HttpMethod.Get, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { "" });
            await ExecuteTest(HttpMethod.Get, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Get, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }, shouldSucceeed: true);
        }

        [TestMethod]
        public async Task TestLoadAllSecurity()
        {
            await ExecuteTest(HttpMethod.Get, "/models/mPerson", new string[] { "" });
            await ExecuteTest(HttpMethod.Get, "/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Get, "/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD });
            await ExecuteTest(HttpMethod.Get, "/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD_ALL }, shouldSucceeed: true);
        }

        [TestMethod]
        public async Task TestDeleteSecurity()
        {
            await ExecuteTest(HttpMethod.Delete, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { "" });
            await ExecuteTest(HttpMethod.Delete, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Delete, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD });
            await ExecuteTest(HttpMethod.Delete, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.DELETE }, shouldSucceeed: true);
        }

        [TestMethod]
        public async Task TestUpdateSecurity()
        {
            await ExecuteTest(HttpMethod.Patch, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { "" });
            await ExecuteTest(HttpMethod.Patch, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Patch, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD });
            await ExecuteTest(HttpMethod.Patch, $"/models/mPerson/{mPersonHandler.Persons[0].id}", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.UPDATE }, shouldSucceeed: true, parameters: new Hashtable() { { "FirstName", "Testing123" } });
        }

        [TestMethod]
        public async Task TestSaveSecurity()
        {
            await ExecuteTest(HttpMethod.Put, $"/models/mPerson", new string[] { "" });
            await ExecuteTest(HttpMethod.Put, $"/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Put, $"/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD });
            await ExecuteTest(HttpMethod.Put, $"/models/mPerson", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.SAVE }, shouldSucceeed: true, parameters: new Hashtable() { { "FirstName", "Testing123" }, { "LastName", "Testing1234" }, { "BirthDay", DateTime.Now } });
        }

        [TestMethod]
        public async Task TestListMethodSecurity()
        {
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/Search", new string[] { "" }, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            });
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/Search", new string[] { Constants.Rights.CAN_ACCESS }, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            });
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/Search", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.SEARCH }, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }, shouldSucceeed:true);
        }

        [TestMethod]
        public async Task TestInstanceMethodSecurity()
        {
            await ExecuteTest(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", new string[] { "" });
            await ExecuteTest(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", new string[] { Constants.Rights.CAN_ACCESS });
            await ExecuteTest(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD });
            await ExecuteTest(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.METHOD }, shouldSucceeed:true);
        }

        [TestMethod]
        public async Task TestStaticMethodSecurity()
        {
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/FormatName", new string[] { "" }, parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" } });
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/FormatName", new string[] { Constants.Rights.CAN_ACCESS }, parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" } });
            await ExecuteTest(HttpMethod.Post, "/models/mPerson/FormatName", new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.STATIC_METHOD }, parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" } }, shouldSucceeed:true);
        }
    }
}
