using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
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

        [TestInitialize]
        public void Init()
        {
            _middleware = new VueMiddleware(null, new VueMiddlewareOptions(modelsOptions: new VueModelsOptions(new SecureSession(), ignoreInvalidModels: true)));
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
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, parameters: new Hashtable() { { "FirstName", "Testing123" } }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
            Assert.AreEqual(firstName, mPerson.Persons[0].FirstName);
        }
    }
}
