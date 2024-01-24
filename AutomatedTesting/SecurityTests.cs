using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.IO;

namespace AutomatedTesting
{
    [TestClass]
    public class SecurityTests
    {
        public const string _NOT_ALLOWED_MESSAGE = "Not Authorized";
        public const int _NOT_ALLOWED_STATUS = 403;
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
        public void TestModelLevelSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status,session:new Security.SecureSession(new string[] {""}))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE,content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestLoadSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS,Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestLoadAllSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS,Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD_ALL }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestDeleteSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}",new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.DELETE }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestUpdateSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PATCH", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.UPDATE }),parameters:new Hashtable() { { "FirstName", "Testing123" } })).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestSaveSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.SAVE }), parameters: new Hashtable() { { "FirstName", "Testing123" }, { "LastName", "Testing1234" }, { "BirthDay", DateTime.Now } })).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestListMethodSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.SEARCH }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestInstanceMethodSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.METHOD }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestStaticMethodSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, session: new Security.SecureSession(new string[] { "" }), parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" } })).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }), parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" } })).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/FormatName", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.STATIC_METHOD }), parameters: new Hashtable() { { "firstName", "Testing123" }, { "lastName", "Testing1234" }})).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }
    }
}
