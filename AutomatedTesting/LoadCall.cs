using AutomatedTesting.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public  class LoadCall
    {
        private RequestHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
        }

        [TestMethod]
        public void TestLoadPerson()
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Request.Path = new PathString(String.Format("/models/mPerson/{0}", new object[] { mPerson.Persons[0].id }));
            context.Response.Body = new MemoryStream();

            Assert.IsTrue(_handler.HandlesRequest(context));
            _handler.ProcessRequest(context, null).Wait();
            context.Response.Body.Position = 0;
            string content = new StreamReader(context.Response.Body).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Assert.IsTrue(content.Contains(String.Format("\"id\":\"{0}\"", new object[] { mPerson.Persons[0].id })));
        }
    }
}
