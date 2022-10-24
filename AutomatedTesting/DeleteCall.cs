using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class DeleteCall
    {
        private VueHandlerMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware = new VueHandlerMiddleware(null, new VueHandlerOptions(new SecureSession(),ignoreInvalidModels:true));
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
    }
}
