using AutomatedTesting.Models;
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
        private RequestHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _handler.Dispose();
        }

        [TestMethod]
        public void TestDeleteMethod()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("DELETE", String.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _handler, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
            Assert.AreNotEqual(personCount, mPerson.Persons.Length);
        }
    }
}
