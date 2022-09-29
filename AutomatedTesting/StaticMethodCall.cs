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
    public class StaticMethodCall
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
        public void TestStaticMethod()
        {
            string firstName = "Testing123";
            string lastName = "Testing1234";
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _handler, out status, parameters: new Hashtable() { 
                { "firstName", firstName }, 
                { "lastName", lastName } 
            })).ReadToEnd();
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.FormatName(null,lastName,firstName),content);
        }
    }
}
