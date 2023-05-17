using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class DeleteCall
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
        public void TestDeleteMethod()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("DELETE", String.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
            Assert.AreNotEqual(personCount, mPerson.Persons.Length);
        }

        [TestMethod]
        public void TestDeleteMethodWithMissingModel()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadResponse(Utility.ExecuteRequest("DELETE", "/models/mPerson/0", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.AreEqual(404, status);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual("Model Not Found", result);
            Assert.AreEqual(personCount, mPerson.Persons.Length);
        }
    }
}
