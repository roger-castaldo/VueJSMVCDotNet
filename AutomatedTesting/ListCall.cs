using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public  class ListCall
    {
        public interface ILog
        {
            void Log(LogLevel level, string message);
        }

        private class Log : ILogger,IDisposable
        {
            private readonly ILog log;

            public Log(ILog log)
            {
                this.log=log;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull
            {
                return this;
            }

            public void Dispose()
            {
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var msg = formatter(state, exception);
                System.Diagnostics.Debug.WriteLine($"{logLevel}:{msg}");
                log.Log(logLevel,msg);
            }
        }


        private VueMiddleware _middleware;
        private Mock<ILog> _writer;
        private IDataStore _store;

        [TestInitialize]
        public void Init()
        {
            _writer = new Mock<ILog>();
            _middleware =Utility.CreateMiddleware(true,logWriter:new Log(_writer.Object));
            _store=new DataStore();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status,parameters:new Hashtable()
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
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
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
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
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
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/Search", _middleware, out status, parameters: new Hashtable()
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
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/ListBobs", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Count(p => p.FirstName.ToLower()=="bob"), ((ArrayList)result).Count);
        }

        [TestMethod]
        public void TestPagedParameterlessList()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("LIST", "/models/mPerson/ListBobsPaged", _middleware, out status, parameters: new Hashtable()
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

        private void _TestParameterListCall(string url,Hashtable pars,int? expectedStatus=null)
        {
            int status;
            MemoryStream ms = Utility.ExecuteRequest("LIST", url, _middleware, out status,parameters:pars);
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
            _TestParameterListCall("/models/mPerson/ListByDate",new Hashtable()
            {
                {"date",DateTime.Now }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Date"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByDate", new Hashtable()
            {
                {"date","invalid" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Date"), Times.Once);
        }

        [TestMethod()]
        public void TestListIntegerParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Integer"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",long.MinValue }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Integer"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Integer"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",int.MaxValue}
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Integer"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByInt", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Integer"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListLongParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Long"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(long.MaxValue),new BigInteger(1))}
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Long"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Long"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",long.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Long"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByLong", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Long"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListShortParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Short"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",int.MaxValue }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Short"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Short"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",short.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Short"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByShort", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Short"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListByteParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Byte"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",short.MaxValue }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Byte"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Byte"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",byte.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Byte"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByByte", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Byte"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListUIntegerParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UInteger"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",ulong.MaxValue}
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UInteger"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UInteger"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",uint.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UInteger"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByUInt", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UInteger"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListULongParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By ULong"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",BigInteger.Add(new BigInteger(ulong.MaxValue), new BigInteger(1)) }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By ULong"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By ULong"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",ulong.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By ULong"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByULong", new Hashtable()
            {
                {"val",0}
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By ULong"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListUShortParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UShort"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",uint.MaxValue }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UShort"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UShort"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",ushort.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UShort"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByUShort", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By UShort"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListDoubleParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Double"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Double"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",double.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Double"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByDouble", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Double"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListFloatParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Float"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Float"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",float.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Float"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByFloat", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Float"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListDecimalParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Decimal"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MinValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Decimal"), Times.Once);
            _TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",decimal.MaxValue }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Decimal"), Times.Exactly(2));
            _TestParameterListCall("/models/mPerson/ListByDecimal", new Hashtable()
            {
                {"val",0 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Decimal"), Times.Exactly(3));
        }

        [TestMethod()]
        public void TestListGuidParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Guid"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByGuid", new Hashtable()
            {
                {"val",Guid.Empty }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Guid"), Times.Once);
        }

        [TestMethod()]
        public void TestListEnumParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Enum"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByEnum", new Hashtable()
            {
                {"val",mDataTypes.TestEnums.Test1 }
            });
            _writer.Verify(w => w.Log(LogLevel.Trace, "Called List By Enum"), Times.Once);
        }

        [TestMethod()]
        public void TestListBooleanParameter()
        {
            _TestParameterListCall("/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val","p" }
            }, expectedStatus: 404);
            _writer.Verify(w => w.Log(LogLevel.Debug, "Called List By Boolean"), Times.Never);
            _TestParameterListCall("/models/mPerson/ListByBoolean", new Hashtable()
            {
                {"val",true }
            });
            _writer.Verify(w => w.Log(LogLevel.Debug, "Called List By Boolean"), Times.Once);
        }
    }
}
