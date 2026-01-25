using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class DeleteCall
    {
        [TestMethod]
        public async Task TestDeleteMethod()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);
            int personCount = mPersonHandler.Persons.Length;

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Delete, $"/models/mPerson/{mPersonHandler.Persons[0].id}", webApplicationFactory);
            var response = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(response);
            Assert.IsInstanceOfType(response, typeof(bool));
            Assert.IsTrue((bool)response);
            Assert.AreNotEqual(personCount, ((mPerson[])store[mPersonHandler.KEY]).Length);
        }

        [TestMethod]
        public async Task TestDeleteMethodWithMissingModel()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);
            int personCount = mPersonHandler.Persons.Length;

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Delete, "/models/mPerson/0", webApplicationFactory);
            var response = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(response, typeof(bool));
            Assert.IsFalse((bool)response);
            Assert.AreEqual(personCount, ((mPerson[])store[mPersonHandler.KEY]).Length);
        }
    }
}
