﻿using AutomatedTesting.Security;
using Jint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace AutomatedTesting
{
    [TestClass]
    public class Javascript
    {
        private VueMiddleware _middleware;

        [TestInitialize]
        public void Init()
        {
            _middleware = Utility.CreateMiddleware(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _middleware.Dispose();
        }

        private delegate object _delCreateWeakMap();

        [TestMethod]
        public void JavascriptGenerationValidation()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mPerson", content);
                eng.AddModule("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
            }
            catch(Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void JavascriptCompressedGenerationValidation()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.AddModule("mPerson", content);
                eng.AddModule("custom", @"import { mPerson } from 'mPerson';
export const name = 'John';");
                var ns = eng.ImportModule("custom");
                Assert.AreEqual("John", ns.Get("name").AsString());
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
        }

        [TestMethod]
        public void JavascriptCompressionPerformance()
        {
            int status;
            string content = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mPerson.min.js", _middleware, out status)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }
    }
}
