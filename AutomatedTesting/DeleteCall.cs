using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;

namespace AutomatedTesting
{
    [TestClass]
    public class DeleteCall
    {
        private VueMiddleware _middleware;
        private IDataStore _store;

        [TestInitialize]
        public void Init()
        {
            _middleware = Utility.CreateMiddleware(true);
            _store = new DataStore();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestDeleteMethod()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("DELETE", $"/models/mPerson/{mPerson.Persons[0].id}", _middleware, out status,store:_store));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
            Assert.AreNotEqual(personCount, ((mPerson[])_store[mPerson.KEY]).Length);
        }

        [TestMethod]
        public void TestDeleteMethodWithMissingModel()
        {
            int personCount = mPerson.Persons.Length;
            int status;
            object result = Utility.ReadResponse(Utility.ExecuteRequest("DELETE", "/models/mPerson/0", _middleware, out status, store: _store));
            Assert.IsNotNull(result);
            Assert.AreEqual(404, status);
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual("Model Not Found", result);
            Assert.IsNull(_store[mPerson.KEY]);
        }
    }
}
