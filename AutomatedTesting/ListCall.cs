﻿using AutomatedTesting.Models;
using Jint;
using Microsoft.AspNetCore.Http;
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
    public  class ListCall
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
        public void TestList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=10", _handler,out status));
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
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=b&PageStartIndex=0&PageSize=10", _handler, out status));
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
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=0&PageSize=2", _handler, out status));
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
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET", "/search/mPerson?q=NULL&PageStartIndex=1&PageSize=2", _handler, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Length / (decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual((int)Math.Min(mPerson.Persons.Length-2,2), ((ArrayList)((Hashtable)result)["response"]).Count);
        }
    }
}
