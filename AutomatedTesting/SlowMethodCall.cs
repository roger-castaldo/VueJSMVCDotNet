using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class SlowMethodCall
    {
        [TestMethod]
        public async Task TestSlowMethod()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/GetSlowTimespan", webApplicationFactory);
            var url = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            bool done = false;
            string result = null;
            int cnt = 0;
            //Act2
            while (!done && cnt < 5)
            {
                (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, (string)url, webApplicationFactory);
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
                    System.Threading.Thread.Sleep(1000);
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
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/SlowAddCall", webApplicationFactory);
            var url = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            bool done = false;
            string result = null;
            int cnt = 0;
            //Act2
            ArrayList data = new ArrayList();
            while (!done && cnt < 10)
            {
                (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, (string)url, webApplicationFactory);
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
                    System.Threading.Thread.Sleep(1000);
                    cnt++;
                }
            }
            Assert.IsTrue(done);
            Assert.IsTrue(data.Count == 6);
            Assert.AreEqual("5", data[5].ToString());
        }

        [TestMethod]
        public async Task TestSlowMethodWithError()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/GetSlowException", webApplicationFactory);
            var url = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);
            bool done = false;
            string result = null;
            int cnt = 0;
            //Act2
            while (!done && cnt < 5)
            {
                (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, (string)url, webApplicationFactory);
                if (responseStatus==200)
                {
                    object content = Utility.ReadJSONResponse(responseStream);
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
                        System.Threading.Thread.Sleep(1100);
                        cnt++;
                    }
                }
                else
                {
                    result = new StreamReader(responseStream).ReadToEnd();
                    done=true;
                }
            }
            Assert.IsTrue(done);
            Assert.AreEqual(500, responseStatus);
            Assert.IsNotNull(result);
            Assert.AreEqual("Error", result);
        }

        [TestMethod]
        public async Task TestSlowMethodWithTimeout()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.PersonModelRoute}/GetSlowTimeout", webApplicationFactory);
            var url = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(200, responseStatus);

            Task.Delay(TimeSpan.FromSeconds(61)).Wait();

            //Act2
            (_, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, (string)url, webApplicationFactory);

            Assert.AreEqual(404, responseStatus);
        }
    }
}
