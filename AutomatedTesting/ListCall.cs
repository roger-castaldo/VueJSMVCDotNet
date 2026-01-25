using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class ListCall
    {
        [TestMethod]
        public async Task TestList()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/Search", webApplicationFactory,
                parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("totalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("data"));
            Assert.AreEqual(1, int.Parse(((Hashtable)result)["totalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["data"], typeof(ArrayList));
            Assert.AreEqual(mPersonHandler.Persons.Length, ((ArrayList)((Hashtable)result)["data"]).Count);
        }

        [TestMethod]
        public async Task TestListParameter()
        {
            //Arrange
            var query = 'b';
            var count = mPersonHandler.Persons.Count(p => p.FirstName.Contains(query, StringComparison.InvariantCultureIgnoreCase) || p.LastName.Contains(query, StringComparison.InvariantCultureIgnoreCase));
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/Search", webApplicationFactory,
                parameters: new Hashtable()
            {
                {"q",query },
                {"PageStartIndex",0 },
                {"PageSize",10}
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("totalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("data"));
            Assert.AreEqual(1, int.Parse(((Hashtable)result)["totalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["data"], typeof(ArrayList));
            Assert.AreEqual(count, ((ArrayList)((Hashtable)result)["data"]).Count);
        }

        [TestMethod]
        public async Task TestListPageSize()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/Search", webApplicationFactory,
                parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",2}
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("totalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("data"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPersonHandler.Persons.Length/(decimal)2), int.Parse(((Hashtable)result)["totalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["data"], typeof(ArrayList));
            Assert.AreEqual(2, ((ArrayList)((Hashtable)result)["data"]).Count);
        }

        [TestMethod]
        public async Task TestListPageStartIndex()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/Search", webApplicationFactory,
                parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",1 },
                {"PageSize",2}
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("totalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("data"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPersonHandler.Persons.Length / (decimal)2), int.Parse(((Hashtable)result)["totalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["data"], typeof(ArrayList));
            Assert.AreEqual((int)Math.Min(mPersonHandler.Persons.Length-2, 2), ((ArrayList)((Hashtable)result)["data"]).Count);
        }

        [TestMethod]
        public async Task TestParameterlessList()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/ListBobs", webApplicationFactory);
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPersonHandler.Persons.Count(p => p.FirstName.ToLower()=="bob"), ((ArrayList)result).Count);
        }

        [TestMethod]
        public async Task TestPagedParameterlessList()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, "/models/mPerson/ListBobsPaged", webApplicationFactory,
                parameters: new Hashtable()
            {
                {"pageStartIndex",0 },
                {"pageSize",2}
            });
            var result = Utility.ReadJSONResponse(responseStream);

            //Assert
            Assert.AreEqual(200, responseStatus);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("totalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("data"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPersonHandler.Persons.Count(p => p.FirstName.ToLower()=="bob")/(decimal)2), int.Parse(((Hashtable)result)["totalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["data"], typeof(ArrayList));
            Assert.IsTrue(((ArrayList)((Hashtable)result)["data"]).Count<=2);
        }

        private async Task TestParameterListCallAsync(WebApplicationFactory<Program> webApplicationFactory, string url, Hashtable pars, int? expectedStatus = null)
        {
            var (responseStream, responseStatus, _) = await Utility.ExecuteRequestAsync(HttpMethod.Post, url, webApplicationFactory, parameters: pars);
            if (expectedStatus!=null)
                Assert.AreEqual(expectedStatus.Value, responseStatus);
            else
            {
                object result = Utility.ReadJSONResponse(responseStream);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(ArrayList));
                Assert.AreEqual(mPersonHandler.Persons.Length, ((ArrayList)result).Count);
            }
        }

        [TestMethod()]
        public async Task TestListDateTimeParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDate", new Hashtable()
            {
                {"date",DateTime.UtcNow }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Date"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDate", new Hashtable()
            {
                {"date","invalid" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Date"), Times.Once);
        }

        [TestMethod()]
        public async Task TestListIntegerParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",long.MinValue }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MaxValue}
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListLongParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByLong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(long.MaxValue),new BigInteger(1))}
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListShortParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",int.MaxValue }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListByteParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByByte", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",short.MaxValue }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListUIntegerParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",ulong.MaxValue}
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListULongParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByULong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(ulong.MaxValue), new BigInteger(1)) }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",0}
            });
            writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListUShortParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",uint.MaxValue }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListDoubleParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListFloatParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListDecimalParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MinValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Once);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MaxValue }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Exactly(2));
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",0 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Exactly(3));
        }

        [TestMethod()]
        public async Task TestListGuidParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Guid"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val",Guid.Empty }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Guid"), Times.Once);
        }

        [TestMethod()]
        public async Task TestListEnumParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Enum"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val",mDataTypes.TestEnums.Test1 }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Enum"), Times.Once);
        }

        [TestMethod()]
        public async Task TestListBooleanParameter()
        {
            //Arrange
            var writer = new Mock<ILogger>();
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, logWriter: writer.Object);

            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            writer.VerifyLog(w => w.LogTrace("Called List By Boolean"), Times.Never);
            await TestParameterListCallAsync(webApplicationFactory, "/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val",true }
            });
            writer.VerifyLog(w => w.LogTrace("Called List By Boolean"), Times.Once);
        }
    }
}
