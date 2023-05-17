﻿using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class SlowMethodCall
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
        public void TestSlowMethod()
        {
            int status;
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/GetSlowTimespan", _middleware, out status));
            Assert.IsInstanceOfType(url, typeof(string));
            bool done = false;
            string result = null;
            int cnt = 0;
            while(!done && cnt < 5)
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
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/SlowAddCall", _middleware, out status));
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

        [TestMethod]
        public void TestSlowMethodWithError()
        {
            int status;
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/GetSlowException", _middleware, out status));
            Assert.IsInstanceOfType(url, typeof(string));
            bool done = false;
            string result = null;
            int cnt = 0;
            while (!done && cnt < 5)
            {
                var memoryStream = Utility.ExecuteRequest("PULL", (string)url, _middleware, out status);
                if (status==200)
                {
                    object content = Utility.ReadJSONResponse(memoryStream);
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
                else
                {
                    result = new StreamReader(memoryStream).ReadToEnd();
                    done=true;
                }
            }
            Assert.IsTrue(done);
            Assert.AreEqual(500, status);
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result);
        }

        [TestMethod]
        public void TestSlowMethodWithTimeout()
        {
            int status;
            object url = Utility.ReadJSONResponse(Utility.ExecuteRequest("SMETHOD", "/models/mPerson/GetSlowTimeout", _middleware, out status));
            Assert.IsInstanceOfType(url, typeof(string));
            Task.Delay(TimeSpan.FromSeconds(61)).Wait();

            var result = Utility.ExecuteRequest("PULL", (string)url, _middleware, out status);

            Assert.AreEqual(404, status);
        }
    }
}
