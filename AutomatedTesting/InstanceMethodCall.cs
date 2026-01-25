using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class InstanceMethodCall
    {
        [TestMethod]
        public async Task TestInstanceMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual(mPersonHandler.Persons[0].GetFullName(), content);
        }

        [TestMethod]
        public async Task TestModelListReturnInstanceMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mGroup/{mGroupHandler.Groups[0].id}/Search", webApplicationFactory,
            parameters: new Hashtable()
            {
                {"name",mGroupHandler.Groups[0].People[0].FirstName }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(1, ((ArrayList)result).Count);
        }

        [TestMethod]
        public async Task TestModelReturnInstanceMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mGroup/{mGroupHandler.Groups[0].id}/FindFirst", webApplicationFactory,
            parameters: new Hashtable()
            {
                {"name",mGroupHandler.Groups[0].People[0].FirstName }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.AreEqual(mGroupHandler.Groups[0].People[0].id, ((Hashtable)result)["id"]);
        }

        [TestMethod]
        public async Task TestInstanceMethodWithModelParameter()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mGroup/{mGroupHandler.Groups[0].id}/ContainsPerson", webApplicationFactory,
            parameters: new Hashtable()
            {
                {
                    "person",new Hashtable(){
                        { "id",mGroupHandler.Groups[0].People[0].id }
                    }
                }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
        }

        [TestMethod]
        public async Task TestInstanceMethodWithModelListParameter()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mGroup/{mGroupHandler.Groups[0].id}/ContainsPeople", webApplicationFactory,
            parameters: new Hashtable()
            {
                {
                    "persons",new ArrayList(){
                        new Hashtable(){
                            { "id",mGroupHandler.Groups[0].People[0].id }
                        }
                    }
                }
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.IsTrue((bool)result);
        }

        [TestMethod]
        public async Task TestLoadSecurityBlocked()
        {
            //Arrange
            (var webApplicationFactory, _) = Utility.CreateApplication(true, new SecureSession(new string[] { "" }));

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_STATUS, responseStatus);
        }

        [TestMethod]
        public async Task ModelNotFound()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/0/GetFullName", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual("Model Not Found", content);
        }

        [TestMethod]
        public async Task TestInstanceMethodWithSameNameButParameter()
        {
            //Arrange
            var store = new DataStore();
            store[mPersonHandler.KEY] = mPersonHandler.Persons.ToArray();
            (var webApplicationFactory, _) = Utility.CreateApplication(true, store);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{((mPerson[])store[mPersonHandler.KEY])[0].id}/GetFullName", webApplicationFactory,
                parameters: new Hashtable()
                {
                    {"middleName","John" }
                });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual(((mPerson[])store[mPersonHandler.KEY])[0].GetFullName("John"), content);
        }

        [TestMethod]
        public async Task MethodSecurityBlocked()
        {
            //Arrange
            (var webApplicationFactory, _) = Utility.CreateApplication(true, new SecureSession(new string[] { Constants.Rights.CAN_ACCESS, Constants.Rights.LOAD }));

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_MESSAGE, content);
            Assert.AreEqual(SecurityTests._NOT_ALLOWED_STATUS, responseStatus);
        }

        [TestMethod]
        public async Task TestInvalidMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetFullName", webApplicationFactory,
                parameters: new Hashtable()
                {
                    {"Name","John" }
                }
            );
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual("Unable to locate method with matching parameters", content);
        }

        [TestMethod]
        public async Task TestVoidInstanceMethod()
        {
            var store = new DataStore();
            store[mPersonHandler.KEY] = mPersonHandler.Persons.ToArray();
            (var webApplicationFactory, _) = Utility.CreateApplication(true, store);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{((mPerson[])store[mPersonHandler.KEY])[0].id}/SetFullName", webApplicationFactory,
                parameters: new Hashtable()
                {
                    {"fullName","Bob, Loblaw" }
                });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual(0, content.Length);
            Assert.AreEqual(((mPerson[])store[mPersonHandler.KEY])[0].GetFullName(), "Bob, Loblaw");
        }

        [TestMethod]
        public async Task TestObjectInstanceMethod()
        {
            var store = new DataStore();
            store[mPersonHandler.KEY] = mPersonHandler.Persons.ToArray();
            (var webApplicationFactory, _) = Utility.CreateApplication(true, store);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{((mPerson[])store[mPersonHandler.KEY])[0].id}/IsFullName", webApplicationFactory,
                parameters: new Hashtable()
                {
                    {"fullName", ((mPerson[])store[mPersonHandler.KEY])[0].GetFullName()}
                });
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual("true", content);
        }

        [TestMethod]
        public async Task TestObjectInstanceMethodWithOpImplicitParameter()
        {
            //Arrange
            var store = new DataStore();
            store[mPersonHandler.KEY] = mPersonHandler.Persons.ToArray();
            (var webApplicationFactory, _) = Utility.CreateApplication(true, store);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{((mPerson[])store[mPersonHandler.KEY])[0].id}/IsNameMatch", webApplicationFactory,
                parameters: new Hashtable()
                {
                    {"name",
                        new Hashtable(){
                        {"FirstName", ((mPerson[])store[mPersonHandler.KEY])[0].FirstName},
                        {"LastName", ((mPerson[])store[mPersonHandler.KEY])[0].LastName}
                        }
                    }
                });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public async Task TestException()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/ThrowInstanceException", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(500, responseStatus);
            Assert.IsFalse(string.IsNullOrWhiteSpace(content));
            Assert.AreEqual("Internal Server Error", content);
        }

        [TestMethod]
        public async Task TestSlowMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, _, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/GetInstanceSlowTimespan", webApplicationFactory);
            var url = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsInstanceOfType(url, typeof(string));

            //Act/Assert
            bool done = false;
            string result = null;
            int cnt = 0;
            while (!done && cnt < 5)
            {
                (responseStream, var responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, url, webApplicationFactory);
                if (responseStatus == 500)
                {
                    System.Diagnostics.Trace.WriteLine(await new StreamReader(responseStream).ReadToEndAsync());
                }
                var content = Utility.ReadJSONResponse(responseStream);

                Assert.AreEqual(200, responseStatus);
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
                    await Task.Delay(1000);
                    cnt++;
                }
            }
            Assert.IsTrue(done);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task TestSlowMethodWithAddItem()
        {
            //Arrange
            ArrayList data = new ArrayList();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, _, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"/models/mPerson/{mPersonHandler.Persons[0].id}/InstanceSlowAddCall", webApplicationFactory);
            var url = Utility.ReadJavascriptResponse(responseStream);

            //Assert
            Assert.IsInstanceOfType(url, typeof(string));

            //Act/Assert
            bool done = false;
            int cnt = 0;
            while (!done && cnt < 10)
            {
                (responseStream, var responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, url, webApplicationFactory);
                var content = Utility.ReadJSONResponse(responseStream);
                Assert.AreEqual(200, responseStatus);
                Assert.IsInstanceOfType(content, typeof(Hashtable));
                Assert.IsTrue(((Hashtable)content).ContainsKey("IsFinished"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("HasMore"));
                Assert.IsTrue(((Hashtable)content).ContainsKey("Data"));
                data.AddRange((ArrayList)((Hashtable)content)["Data"]);
                if ((bool)((Hashtable)content)["IsFinished"])
                    done = true;
                else
                {
                    await Task.Delay(1000);
                    cnt++;
                }
            }
            Assert.IsTrue(done);
            Assert.IsTrue(data.Count == 6);
            Assert.AreEqual("5", data[5].ToString());
        }
    }
}
