using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class InstanceMethodCall
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
        public void TestInstanceMethod()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _handler, out status)).ReadToEnd();
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.Persons[0].GetFullName(null), content);
        }
    }
}
