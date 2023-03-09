using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.Extensions.Primitives;
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
        public void TestStaticMethodWithFormData()
        {
            string firstName = "Testing123";
            string lastName = "Testing1234";
            int status;
            string content = Utility.ReadResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, new Dictionary<string, StringValues>()
            {
                {"firstName",firstName },
                {"lastName",lastName }
            }));
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.FormatName(null, lastName, firstName), content);
        }

        [TestMethod]
        public void TestStaticMethodWithFormDataArrayValues()
        {
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };
            int status;
            var content = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatNames", _middleware, out status, new Dictionary<string, StringValues>()
            {
                {"firstName",firstName },
                {"lastName",lastName }
            }));

            Assert.IsInstanceOfType(content, typeof(ArrayList));

            var al = (ArrayList)content;

            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPerson.FormatName(null, lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPerson.FormatName(null, lastName[1], firstName[1]), al[1]);
        }

        [TestMethod]
        public void TestStaticMethodWithFormDataJsonEncoded()
        {
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };
            int status;
            var content = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatNames", _middleware, out status, new Dictionary<string, StringValues>()
            {
                {"firstName:json",JSON.JsonEncode(firstName) },
                {"lastName:json",JSON.JsonEncode(lastName) }
            }));

            Assert.IsInstanceOfType(content, typeof(ArrayList));

            var al = (ArrayList)content;

            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPerson.FormatName(null, lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPerson.FormatName(null, lastName[1], firstName[1]), al[1]);
        }

        [TestMethod]
        public void TestStaticMethodWithFormDataJsonEncodedMultipleValues()
        {
            string[] firstName = new string[] { "Testing123", "Testing456" };
            string[] lastName = new string[] { "Testing1234", "Testing4567" };
            int status;
            var content = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatNames", _middleware, out status, new Dictionary<string, StringValues>()
            {
                {"firstName:json",new string[]{ JSON.JsonEncode(firstName[0]), JSON.JsonEncode(firstName[1]) } },
                {"lastName:json",new string[]{ JSON.JsonEncode(lastName[0]), JSON.JsonEncode(lastName[1]) } }
            }));

            Assert.IsInstanceOfType(content, typeof(ArrayList));

            var al = (ArrayList)content;

            Assert.AreEqual(2, al.Count);
            Assert.AreEqual(mPerson.FormatName(null, lastName[0], firstName[0]), al[0]);
            Assert.AreEqual(mPerson.FormatName(null, lastName[1], firstName[1]), al[1]);
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
            Assert.AreEqual("Unable to locate method with matching parameters", content);
        }

        [TestMethod]
        public void TestStaticMethodWithNullNotNullArguement()
        {
            string firstName = null;
            string lastName = "Testing1234";
            int status;
            string content = Utility.ReadResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, parameters: new Hashtable() {
                { "firstName", firstName },
                { "lastName", lastName }
            }));
            Assert.AreEqual(404, status);
            Assert.AreEqual("Unable to locate method with matching parameters", content);
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
