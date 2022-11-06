using AutomatedTesting.Models;
using AutomatedTesting.Security;
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
        public void TestInstanceMethod()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.Persons[0].GetFullName(null), content);
        }
    }
}
