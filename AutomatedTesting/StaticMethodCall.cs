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
            _middleware = Utility.CreateMiddleware(true);
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

        [TestMethod]
        public void TestStaticMethodNotFound()
        {
            string firstName = "Testing123";
            string lastName = "Testing1234";
            int status;
            string content = Utility.ReadResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, parameters: new Hashtable() {
                { "firstName", firstName },
                { "lastName", lastName },
                {"userName","testing" }
            }));
            Assert.AreEqual(404, status);
            Assert.AreEqual("Unable to locate requested method to invoke",content);
        }

        [TestMethod]
        public void TestStaticMethodWithObjectResult()
        {
            int status;
            var result = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/ProduceObject", _middleware, out status, parameters: new Hashtable() {
                {"isnull",false }
            }));
            Assert.AreEqual(200, status);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
        }

        [TestMethod]
        public void TestStaticMethodWithNullResult()
        {
            int status;
            var result = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/ProduceObject", _middleware, out status, parameters: new Hashtable() {
                {"isnull",true }
            }));
            Assert.AreEqual(200, status);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestStaticVoidMethod()
        {
            int status;
            var result = Utility.ExecuteRequest("SMETHOD", "/models/mPerson/VoidMethodCall", _middleware, out status, parameters: new Hashtable() {
                {"parameter","" }
            });
            Assert.AreEqual(200, status);
            Assert.AreEqual(0,result.Length);
        }
    }
}
