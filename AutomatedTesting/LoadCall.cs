using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class LoadCall
    {
        [TestMethod]
        public async Task TestLoadPerson()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"/models/mPerson/{mPersonHandler.Persons[0].id}", webApplicationFactory);
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.AreEqual(mPersonHandler.Persons[0].id, ((Hashtable)result)["id"]);
        }

        [TestMethod]
        public async Task TestInvalidIDLoadPerson()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/models/mPerson/0", webApplicationFactory);
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestLoadAllPerson()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/models/mPerson", webApplicationFactory);
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPersonHandler.Persons.Length, ((ArrayList)result).Count);
            for (int x = 0; x < mPersonHandler.Persons.Length; x++)
            {
                Assert.IsInstanceOfType(((ArrayList)result)[x], typeof(Hashtable));
                Assert.AreEqual(mPersonHandler.Persons[x].id, ((Hashtable)((ArrayList)result)[x])["id"]);
            }
        }

        /*
NOTE:  Disabling this test because the Promise resolves/await async functionality is not enabled in Jint and now unable to test
        [TestMethod]
        public void TestLoadJavascript()
        {
            string content = new StreamReader(Utility.ExecuteGet($"{Constants.PersonModelRoute}.js", _handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            content = Constants.JAVASCRIPT_BASE + content + string.Format(@"
var mdl = Promise.resolve(new Promise((resolve)=>{{
        App.Models.mPerson.Load('{0}',function(result){{
            resolve(result);
        }});    
    }})
);

if (mdl===null){{
    throw 'unable to load model';
}}else{{
    throw mdl;
}}", new object[] { mPersonHandler.Persons[0].id });
            Engine eng = new Engine();
            try
            {
                eng.Execute(content);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }*/
    }
}
