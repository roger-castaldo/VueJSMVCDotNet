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
        private RequestHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _handler.Dispose();
        }

        private delegate object _delCreateWeakMap();

        [TestMethod]
        public void JavascriptGenerationValidation()
        {
            string content = new StreamReader(Utility.ExecuteGet("/resources/scripts/mPerson.js", _handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = new Engine();
            try
            {
                eng.Execute(@"
    var window={App:{}};
    var App = window.App;
    var Vue = {
        version:'3.0'
    };

    function WeakMap(){
        return {
            _data:{},
            set:function(key,value){
                this._data[key]=value;
            },
            get:function(key){
                return this._data[key];
            }
        }
    };
"+content);
            }catch(Esprima.ParserException e)
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
            string content = new StreamReader(Utility.ExecuteGet("/resources/scripts/mPerson.min.js", _handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            Engine eng = new Engine();
            try
            {
                eng.Execute(@"
    var window={App:{}};
    var App = window.App;
    var Vue = {
        version:'3.0'
    };

    function WeakMap(){
        return {
            _data:{},
            set:function(key,value){
                this._data[key]=value;
            },
            get:function(key){
                return this._data[key];
            }
        }
    };
" + content);
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
            string content = new StreamReader(Utility.ExecuteGet("/resources/scripts/mPerson.js", _handler)).ReadToEnd();
            Assert.IsTrue(content.Length > 0);
            string minContent = new StreamReader(Utility.ExecuteGet("/resources/scripts/mPerson.min.js", _handler)).ReadToEnd();
            Assert.IsTrue(minContent.Length > 0);
            Assert.IsTrue(minContent.Length < content.Length);
        }
    }
}
