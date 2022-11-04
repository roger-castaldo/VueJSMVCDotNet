using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public  class LoadCall
    {
        private VueMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware = new VueMiddleware(null, new VueMiddlewareOptions(modelsOptions: new VueModelsOptions(new SecureSession(), ignoreInvalidModels: true)));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        [TestMethod]
        public void TestLoadPerson()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET",String.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }), _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(Hashtable));
            Assert.AreEqual(mPerson.Persons[0].id, ((Hashtable)result)["id"]);
        }

        [TestMethod]
        public void TestInvalidIDLoadPerson()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET","/models/mPerson/0", _middleware, out status));
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestLoadAllPerson()
        {
            int status;
            object result = Utility.ReadJSONResponse(Utility.ExecuteRequest("GET","/models/mPerson", _middleware, out status));
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ArrayList));
            Assert.AreEqual(mPerson.Persons.Length, ((ArrayList)result).Count);
            for(int x = 0; x < mPerson.Persons.Length; x++)
            {
                Assert.IsInstanceOfType(((ArrayList)result)[x], typeof(Hashtable));
                Assert.AreEqual(mPerson.Persons[x].id, ((Hashtable)((ArrayList)result)[x])["id"]);
            }
        }

        /*
NOTE:  Disabling this test because the Promise resolves/await async functionality is not enabled in Jint and now unable to test
        [TestMethod]
        public void TestLoadJavascript()
        {
            string content = new StreamReader(Utility.ExecuteGet("/resources/scripts/mPerson.js", _handler)).ReadToEnd();
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
}}", new object[] { mPerson.Persons[0].id });
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
