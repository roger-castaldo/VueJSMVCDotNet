using AutomatedTesting.Models.InvalidModels;
using AutomatedTesting.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class ModelValidation
    {
        private static ModelValidationException _LoadExceptions()
        {
            ModelValidationException e = null;
            try
            {
                VueMiddleware middleware = Utility.CreateMiddleware(false);
            }
            catch (ModelValidationException ex)
            {
                e = ex;
            }
            Assert.IsNotNull(e);
            return e;
        }

        [TestMethod]
        public void TestThrowInvalid()
        {
            _LoadExceptions();
        }

        [TestMethod]
        public void TestDisableInvalid()
        {
            VueMiddleware middleware = Utility.CreateMiddleware(true);
            HttpContext context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Host = new HostString("localhost");
            context.Request.IsHttps = false;
            context.Request.Path = new PathString("/resources/scripts/mInvalidModel.js");
            //Assert.IsFalse(handler.HandlesRequest(context));
            context.Request.Path = new PathString("/resources/scripts/mPerson.js");
            //Assert.IsTrue(handler.HandlesRequest(context));
        }

        [TestMethod]
        public void TestModelWithNoRoute()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions.Count(e => e is NoRouteException && ((NoRouteException)e).ModelType==typeof(ModelWithNoRoute)));
        }
    }
}
