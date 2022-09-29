﻿using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class SaveCall
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
        public void TestSaveMethod()
        {
            string firstName = "Testing123";
            string lastName = "Testing321";
            DateTime birthDay = DateTime.Now;
            int currentCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("PUT", "/models/mPerson", _handler, out status, parameters: new Hashtable() { 
                { "FirstName", firstName },
                {"LastName",lastName },
                {"BirthDay",birthDay }
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("id"));
            Assert.AreNotEqual(currentCount, mPerson.Persons.Length);
            mPerson newPer = null;
            foreach (mPerson p in mPerson.Persons)
            {
                if (p.id == (string)((Hashtable)result)["id"])
                {
                    newPer = p;
                    break;
                }
            }
            Assert.IsNotNull(newPer);
            Assert.AreEqual(firstName, newPer.FirstName);
            Assert.AreEqual(lastName, newPer.LastName);
            Assert.AreEqual(birthDay.ToString(), newPer.BirthDay.ToString());
        }
    }
}