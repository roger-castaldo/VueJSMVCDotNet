using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VueJSMVCDotNet;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;

namespace AutomatedTesting
{
    [TestClass]
    public  class ListCall
    {
        private VueMiddleware _middleware;
        private Mock<ILogger> _writer;

        [TestInitialize]
        public void Init()
        {
            _writer = new Mock<ILogger>();
            _middleware =Utility.CreateMiddleware(true,logWriter:_writer.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestList()
        {
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out _, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)1, int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Length, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListParameter()
        {
            int cnt = 0;
            foreach (mPerson p in mPerson.Persons)
            {
                if (p.FirstName.ToLower().Contains("b") || p.LastName.ToLower().Contains("b"))
                    cnt++;
            }

            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out _, parameters: new Hashtable()
            {
                {"q","b" },
                {"PageStartIndex",0 },
                {"PageSize",10}
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)1, int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(cnt, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListPageSize()
        {
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out _, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",0 },
                {"PageSize",2}
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Length/(decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual(2, ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestListPageStartIndex()
        {
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out _, parameters: new Hashtable()
            {
                {"q",null },
                {"PageStartIndex",1 },
                {"PageSize",2}
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Length / (decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.AreEqual((int)Math.Min(mPerson.Persons.Length-2,2), ((ArrayList)((Hashtable)result)["response"]).Count);
        }

        [TestMethod]
        public void TestParameterlessList()
        {
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/ListBobs", _middleware, out int status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Count(p => p.FirstName.ToLower()=="bob"), ((ArrayList)result).Count);
        }

        [TestMethod]
        public void TestPagedParameterlessList()
        {
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/ListBobsPaged", _middleware, out int status, parameters: new Hashtable()
            {
                {"PageStartIndex",0 },
                {"PageSize",2}
            }));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.IsTrue(((Hashtable)result).ContainsKey("TotalPages"));
            Assert.IsTrue(((Hashtable)result).ContainsKey("response"));
            Assert.AreEqual((int)Math.Ceiling((decimal)mPerson.Persons.Count(p => p.FirstName.ToLower()=="bob")/(decimal)2), int.Parse(((Hashtable)result)["TotalPages"].ToString()));
            Assert.IsInstanceOfType(((Hashtable)result)["response"], typeof(ArrayList));
            Assert.IsTrue(((ArrayList)((Hashtable)result)["response"]).Count<=2);
        }

        private void TestParameterListCall(string url,Hashtable pars,int? expectedStatus=null)
        {
            MemoryStream ms = Utility.ExecuteRequest("LIST", url, _middleware, out int status, parameters: pars);
            if (expectedStatus!=null)
                Assert.AreEqual(expectedStatus.Value, status);
            else
            {
                object result = Utility.ReadJSONResponse(ms);
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(ArrayList));
                Assert.AreEqual(mPerson.Persons.Length, ((ArrayList)result).Count);
            }
        }

        [TestMethod()]
        public void TestListDateTimeParameter()
        {
            TestParameterListCall("/models/mPerson/ListByDate",new Hashtable()
            {
                {"date",DateTime.Now }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Date"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByDate", new Hashtable()
            {
                {"date","invalid" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Date"), Times.Once);
        }

        [TestMethod()]
        public void TestListIntegerParameter()
        {
            TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",long.MinValue }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MaxValue}
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Integer"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListLongParameter()
        {
            TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(long.MaxValue),new BigInteger(1))}
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Long"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListShortParameter()
        {
            TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",int.MaxValue }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Short"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListByteParameter()
        {
            TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",short.MaxValue }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Byte"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListUIntegerParameter()
        {
            TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",ulong.MaxValue}
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UInteger"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListULongParameter()
        {
            TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(ulong.MaxValue), new BigInteger(1)) }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",0}
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By ULong"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListUShortParameter()
        {
            TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",uint.MaxValue }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By UShort"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListDoubleParameter()
        {
            TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Double"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListFloatParameter()
        {
            TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Float"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListDecimalParameter()
        {
            TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MinValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Once);
            TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MaxValue }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Exactly(2));
            TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",0 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Decimal"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListGuidParameter()
        {
            TestParameterListCall("/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Guid"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val",Guid.Empty }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Guid"), Times.Once);
        }

        [TestMethod()]
        public void TestListEnumParameter()
        {
            TestParameterListCall("/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Enum"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val",mDataTypes.TestEnums.Test1 }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Enum"), Times.Once);
        }

        [TestMethod()]
        public void TestListBooleanParameter()
        {
            TestParameterListCall("/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.VerifyLog(w => w.LogTrace("Called List By Boolean"), Times.Never);
            TestParameterListCall("/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val",true }
            });
            _writer.VerifyLog(w => w.LogTrace("Called List By Boolean"), Times.Once);
        }
    }
}
