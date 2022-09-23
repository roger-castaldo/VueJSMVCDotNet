using AutomatedTesting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public  class LoadCall
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
        public void TestLoadPerson()
        {
            string content = new StreamReader(Utility.ExecuteGet(String.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }),_handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains(String.Format("\"id\":\"{0}\"", new object[] { mPerson.Persons[0].id })));
        }

        [TestMethod]
        public void TestInvalidIDLoadPerson()
        {
            string content = new StreamReader(Utility.ExecuteGet("/models/mPerson/0",_handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content=="null");
        }
    }
}
