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
            Assert.AreEqual(status, 200);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.Persons[0].GetFullName(new SecureSession()), content);
        }

        [TestMethod]
        public void TestLoadSecurityBlocked()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { "" }))).ReadToEnd();
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void ModelNotFound()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", "/models/mPerson/0/GetFullName", _middleware, out status)).ReadToEnd();
            Assert.AreEqual(status, 400);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual("Model Not Found", content);
        }

        [TestMethod]
        public void TestInstanceMethodWithSameNameButParameter()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status,
                parameters:new Hashtable()
                {
                    {"middleName","John" }
                })).ReadToEnd();
            Assert.AreEqual(status, 200);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(mPerson.Persons[0].GetFullName("John"), content);
        }

        [TestMethod]
        public void MethodSecurityBlocked()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status, session: new Security.SecureSession(new string[] { Constants.Rights.CAN_ACCESS,Constants.Rights.LOAD }))).ReadToEnd();
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_STATUS, status);
        }

        [TestMethod]
        public void TestInvalidMethod()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status,
                parameters: new Hashtable()
                {
                    {"Name","John" }
                })).ReadToEnd();
            Assert.AreEqual(status, 400);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual("Unable to locate requested method to invoke", content);
        }

        [TestMethod]
        public void TestVoidInstanceMethod()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/SetFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status,
                parameters: new Hashtable()
                {
                    {"fullName","Bob, Loblaw" }
                })).ReadToEnd();
            Assert.AreEqual(status, 200);
            Assert.IsTrue(content.Length==0);
            Assert.AreEqual(mPerson.Persons[0].GetFullName(new SecureSession()), "Bob, Loblaw");
        }

        [TestMethod]
        public void TestObjectInstanceMethod()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/IsFullName", new object[] { mPerson.Persons[0].id }), _middleware, out status,
                parameters: new Hashtable()
                {
                    {"fullName", mPerson.Persons[0].GetFullName(new SecureSession())}
                })).ReadToEnd();
            Assert.AreEqual(status, 200);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(content,"true");
        }

        [TestMethod]
        public void TestException()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/ThrowInstanceException", new object[] { mPerson.Persons[0].id }), _middleware, out status)).ReadToEnd();
            Assert.AreEqual(status, 500);
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(content, "Error");
        }

        [TestMethod]
        public void TestSlowMethod()
        {
            int status;
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/GetInstanceSlowTimespan", new object[] { mPerson.Persons[0].id }), _middleware, out status));
            Assert.IsInstanceOfType(url, typeof(string));
            bool done = false;
            string result = null;
            int cnt = 0;
            while (!done && cnt < 5)
            {
                object content = Utility.ReadJSONResponse(Utility.ExecuteRequest("PULL", (string)url, _middleware, out status));
                Assert.IsInstanceOfType(content, typeof(Hashtable));
                Assert.IsTrue(((Hashtable)content).ContainsKey("IsFinished"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("HasMore"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("Data"));
                if ((bool)((Hashtable)content)["IsFinished"])
                {
                    done = true;
                    Assert.IsInstanceOfType(((Hashtable)content)["Data"], typeof(ArrayList));
                    Assert.IsTrue(((ArrayList)((Hashtable)content)["Data"]).Count == 1);
                    Assert.IsInstanceOfType(((ArrayList)((Hashtable)content)["Data"])[0], typeof(string));
                    result = (string)((ArrayList)((Hashtable)content)["Data"])[0];
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                    cnt++;
                }
            }
            Assert.IsTrue(done);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestSlowMethodWithAddItem()
        {
            ArrayList data = new ArrayList();
            int status;
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("METHOD", string.Format("/models/mPerson/{0}/InstanceSlowAddCall", new object[] { mPerson.Persons[0].id }), _middleware, out status));
            Assert.IsInstanceOfType(url, typeof(string));
            bool done = false;
            int cnt = 0;
            while (!done && cnt < 10)
            {
                object content = Utility.ReadJSONResponse(Utility.ExecuteRequest("PULL", (string)url, _middleware, out status));
                Assert.IsInstanceOfType(content, typeof(Hashtable));
                Assert.IsTrue(((Hashtable)content).ContainsKey("IsFinished"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("HasMore"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("Data"));
                data.AddRange((ArrayList)((Hashtable)content)["Data"]);
                if ((bool)((Hashtable)content)["IsFinished"])
                    done = true;
                else
                {
                    System.Threading.Thread.Sleep(1000);
                    cnt++;
                }
            }
            Assert.IsTrue(done);
            Assert.IsTrue(data.Count == 6);
            Assert.AreEqual("5", data[5].ToString());
        }
    }
}
