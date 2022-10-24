using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class SecurityTests
    {
        private const string _NOT_ALLOWED_MESSAGE = "Not Authorized";
        private const int _NOT_ALLOWED_STATUS = 403;
        private VueHandlerMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware = new VueHandlerMiddleware(null, new VueHandlerOptions(new SecureSession(), ignoreInvalidModels: true));
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
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=10", _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=10", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=10", _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.SEARCH }))).ReadToEnd();
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
