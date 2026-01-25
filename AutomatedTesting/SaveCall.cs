using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class SaveCall
    {
        [TestMethod]
        public async Task TestSaveMethod()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);
            string firstName = "Testing123";
            string lastName = "Testing321";
            DateTime birthDay = DateTime.UtcNow;
            int currentCount = mPersonHandler.Persons.Length;

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Put, Constants.PersonModelRoute, webApplicationFactory,
                parameters: new Hashtable() {
                { "FirstName", firstName },
                {"LastName",lastName },
                {"BirthDay",birthDay }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreNotEqual(currentCount, ((mPerson[])store[mPersonHandler.KEY]).Length);
            mPerson newPer = null;
            foreach (mPerson p in ((mPerson[])store[mPersonHandler.KEY]))
            {
                if (p.id == (string)result)
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
        public async Task TestSaveMethodFailure()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);
            string firstName = "DoNotSave";
            string lastName = "Testing321";
            DateTime birthDay = DateTime.UtcNow;
            int currentCount = mPersonHandler.Persons.Length;

            //Act
            var (responseStream, _, _)= await Utility.ExecuteRequestAsync(HttpMethod.Put, Constants.PersonModelRoute, webApplicationFactory,
                parameters: new Hashtable() {
                { "FirstName", firstName },
                {"LastName",lastName },
                {"BirthDay",birthDay }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.IsNull(result);
            Assert.AreEqual(currentCount, mPersonHandler.Persons.Length);
        }
    }
}
