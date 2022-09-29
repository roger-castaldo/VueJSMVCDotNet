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
    public class ModelValidation
    {
        [TestMethod]
        public void TestThrowInvalid()
        {
            Exception e=null;
            try
            {
                RequestHandler handler = new RequestHandler(RequestHandler.StartTypes.ThrowInvalidExceptions, null);
            }catch(Exception ex)
            {
                e = ex;
            }
            Assert.IsNotNull(e);
        }

        [TestMethod]
        public void TestDisableInvalid()
        {
            RequestHandler handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Request.Path = new PathString("/resources/scripts/mInvalidModel.js");
            Assert.IsFalse(handler.HandlesRequest(context));
            context.Request.Path = new PathString("/resources/scripts/mPerson.js");
            Assert.IsTrue(handler.HandlesRequest(context));
        }
    }
}
