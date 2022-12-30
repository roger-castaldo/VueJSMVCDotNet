using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
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
    public  class ListCall
    {
        private VueMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware =Utility.CreateMiddleware(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=10", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)1, int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Length, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListParameter()
        {
            int cnt = 0;
            foreach (mPerson p in mPerson.Persons)
            {
                if (p.FirstName.ToLower().Contains("b") || p.LastName.ToLower().Contains("b"))
                    cnt++;
            }
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=b&PageStartIndex=0&PageSize=10", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)1, int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(cnt, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListPageSize()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=2", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Length/(decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(2, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListPageStartIndex()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=1&PageSize=2", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Length / (decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual((int)Math.Min(mPerson.Persons.Length-2,2), ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestParameterlessList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/list/mPerson/bob", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Count(p => p.FirstName.ToLower()=="bob"), ((ArrayList)result).Count);
        }

        [TestMethod]
        public void TestPagedParameterlessList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/list/mPerson/bob/pages?PageStartIndex=0&PageSize=2", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Count(p => p.FirstName.ToLower()=="bob")/(decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.IsTrue(((ArrayList)((Hashtable)result)["response"]).Count<=2);
        }

        private void _TestParameterListCall(string url,int? expectedStatus=null)
        {
            int status;
            MemoryStream ms = Utility.ExecuteRequest("GET", url, _middleware, out status);
            if (expectedStatus!=null)
                Assert.AreEqual(expectedStatus.Value, status);
            else
            {
                object result = Utility.ReadJSONResponse(ms);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(ArrayList));
                Assert.AreEqual(mPerson.Persons.Length, ((ArrayList)result).Count);
            }
        }

        [TestMethod()]
        public void TestListDateTimeParameter()
        {
            _TestParameterListCall(string.Format("/list/mPerson/bob/date?par={0}", 50000));
            _TestParameterListCall(string.Format("/list/mPerson/bob/date?par={0}", "p"), expectedStatus: 404);
        }

        [TestMethod()]
        public void TestListIntegerParameter()
        {
            _TestParameterListCall(string.Format("/list/mPerson/bob/int?par={0}", int.MinValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/int?par={0}", int.MaxValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/int?par={0}", 0));
        }

        [TestMethod()]
        public void TestListLongParameter()
        {
            _TestParameterListCall(string.Format("/list/mPerson/bob/long?par={0}", long.MinValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/long?par={0}", long.MaxValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/long?par={0}", 0));
        }

        [TestMethod()]
        public void TestListShortParameter()
        {
            _TestParameterListCall(string.Format("/list/mPerson/bob/short?par={0}", short.MinValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/short?par={0}", short.MaxValue));
            _TestParameterListCall(string.Format("/list/mPerson/bob/short?par={0}", 0));
        }
    }
}
