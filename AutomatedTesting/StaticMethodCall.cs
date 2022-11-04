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
    public class StaticMethodCall
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
        public void TestStaticMethod()
        {
            string firstName = "Testing123";
            string lastName = "Testing1234";
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, parameters: new Hashtable() { 
                { "firstName", firstName }, 
                { "lastName", lastName } 
            })).ReadToEnd();
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.FormatName(null,lastName,firstName),content);
        }
    }
}
