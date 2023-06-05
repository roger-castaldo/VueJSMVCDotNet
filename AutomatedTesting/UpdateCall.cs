using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class UpdateCall
    {
        private VueMiddleware _middleware;
        private IDataStore _store;

        [TestInitialize]
        public void Init()
        {
            _middleware = Utility.CreateMiddleware(true);
            _store=new DataStore();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestUpdateMethod()
        {
            string firstName = "Testing123";
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, parameters: new Hashtable() { { "FirstName", "Testing123" } },store:_store));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
            Assert.AreEqual(firstName, ((mPerson[])_store[mPerson.KEY])[0].FirstName);
        }

        [TestMethod]
        public void TestUpdateMethodWithMissingModel()
        {
            int status;
            object result = Utility.ReadResponse(Utility.ExecuteRequest("PATCH", "/models/mPerson/0", _middleware, out status, parameters: new Hashtable() { { "FirstName", "Testing123" } },store:_store));
            Assert.IsNotNull(result);
            Assert.AreEqual(404, status);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual("Model Not Found", result);
        }
    }
}
