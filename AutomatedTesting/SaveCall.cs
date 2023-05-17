using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class SaveCall
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
        public void TestSaveMethod()
        {
            string firstName = "Testing123";
            string lastName = "Testing321";
            DateTime birthDay = DateTime.Now;
            int currentCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, parameters: new Hashtable() { 
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

        [TestMethod]
        public void TestSaveMethodFailure ()
        {
            string firstName = "DoNotSave";
            string lastName = "Testing321";
            DateTime birthDay = DateTime.Now;
            int currentCount = mPerson.Persons.Length;
            int status;
            var result = Utility.ReadResponse(Utility.ExecuteRequest("PUT", "/models/mPerson", _middleware, out status, parameters: new Hashtable() {
                { "FirstName", firstName },
                {"LastName",lastName },
                {"BirthDay",birthDay }
            }));
            Assert.IsNotNull(result);
            Assert.AreEqual(500, status);
            Assert.AreEqual(currentCount, mPerson.Persons.Length);
        }
    }
}
