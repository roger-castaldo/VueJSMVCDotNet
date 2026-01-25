using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class UpdateCall
    {
        [TestMethod]
        public async Task TestUpdateMethod()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);
            string firstName = "Testing123";

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Patch, $"/models/mPerson/{mPersonHandler.Persons[0].id}", webApplicationFactory,
                parameters: new Hashtable() { { "FirstName", "Testing123" } });
            var response = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(response);
            Assert.IsInstanceOfType(response, typeof(bool));
            Assert.IsTrue((bool)response);
            Assert.AreEqual(firstName, ((mPerson[])store[mPersonHandler.KEY])[0].FirstName);

        }

        [TestMethod]
        public async Task TestUpdateMethodWithMissingModel()
        {
            //Arrange
            (var webApplicationFactory, var store, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Patch, "/models/mPerson/0", webApplicationFactory,
                parameters: new Hashtable() { { "FirstName", "Testing123" } });
            var response = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Model Not Found", response);
            Assert.IsNull(store[mPersonHandler.KEY]);
        }
    }
}
