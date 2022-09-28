using AutomatedTesting.Models;
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
        public void TestModelLevelSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _handler, out status,session:new Security.SecureSession(new string[] {""}))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE,content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/resources/scripts/mPerson.js", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestLoadSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _handler, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson/0", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS,Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestLoadAllSecurity()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _handler, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS,Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("GET", "/models/mPerson", _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD_ALL }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestDeleteSecurity()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}",new object[] { mPerson.Persons[0].id }), _handler, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(_NOT_ALLOWED_STATUS, status);
            content = new StreamReader(Utility.ExecuteRequest("DELETE", string.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _handler, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD, Constants.Rights.DELETE }))).ReadToEnd();
            Assert.AreNotEqual(_NOT_ALLOWED_MESSAGE, content);
            Assert.AreNotEqual(_NOT_ALLOWED_STATUS, status);
            Assert.AreNotEqual(personCount, mPerson.Persons.Length);
        }

    }
}
