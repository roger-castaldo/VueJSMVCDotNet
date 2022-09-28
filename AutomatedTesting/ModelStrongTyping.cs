using Jint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace AutomatedTesting
{
    [TestClass]
    public class ModelStrongTyping
    {
        private string _content;

        [TestInitialize]
        public void Init()
        {
            RequestHandler handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
            int status;
            _content = Constants.JAVASCRIPT_BASE+new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mDataTypes.js", handler,out status)).ReadToEnd()+@"

var mdl = App.Models.mDataTypes.createInstance();
";
        }

        [TestCleanup]
        public void Cleanup()
        {
            _content = null;
        }

        [TestMethod]
        public void TestStringProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.StringField='A string';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.StringField={};");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set StringField: invalid type: Value not a string and cannot be converted");
            }
            try
            {
                eng.Execute(_content + "mdl.StringField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set StringField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullStringProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullStringField='A string';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullStringField={};");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullStringField: invalid type: Value not a string and cannot be converted");
            }
            try
            {
                eng.Execute(_content + "mdl.NullStringField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestCharProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content+"mdl.CharField='A';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.CharField='AB';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message,"Cannot set CharField: invalid type: Value not a char");
            }
            try
            {
                eng.Execute(_content + "mdl.CharField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set CharField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullCharProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullCharField='A';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullCharField='AB';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullCharField: invalid type: Value not a char");
            }
            try
            {
                eng.Execute(_content + "mdl.NullCharField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestShortProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.ShortField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.ShortField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ShortField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.ShortField={0};", (int)short.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ShortField: invalid type: Value is a number, but is too large for a Int16");
            }
            try
            {
                eng.Execute(_content + "mdl.ShortField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ShortField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullShortProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullShortField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullShortField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullShortField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullShortField={0};", (int)short.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullShortField: invalid type: Value is a number, but is too large for a Int16");
            }
            try
            {
                eng.Execute(_content + "mdl.NullShortField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestUShortProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.UShortField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.UShortField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UShortField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.UShortField={0};", (int)ushort.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UShortField: invalid type: Value is a number, but is too large for a UInt16");
            }
            try
            {
                eng.Execute(_content + "mdl.UShortField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UShortField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullUShortProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullUShortField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullUShortField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullUShortField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullUShortField={0};", (int)ushort.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullUShortField: invalid type: Value is a number, but is too large for a UInt16");
            }
            try
            {
                eng.Execute(_content + "mdl.NullUShortField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestByteProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.ByteField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.ByteField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ByteField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.ByteField={0};", (int)byte.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ByteField: invalid type: Value is a number, but is too large for a Byte");
            }
            try
            {
                eng.Execute(_content + "mdl.ByteField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ByteField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullByteProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullByteField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullByteField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullByteField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullByteField={0};", (int)byte.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullByteField: invalid type: Value is a number, but is too large for a Byte");
            }
            try
            {
                eng.Execute(_content + "mdl.NullByteField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestBooleanProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.BooleanField=false;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.BooleanField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set BooleanField: invalid type: Value not boolean and cannot be converted");
            }
            try
            {
                eng.Execute(_content + @"mdl.BooleanField=null;
if (mdl.BooleanField!==false){ throw 'unable to set null boolean to convert to false'; }");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestNullBooleanProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullBooleanField=false;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullBooleanField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullBooleanField: invalid type: Value not boolean and cannot be converted");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullBooleanField=null;
if (mdl.NullBooleanField!==null){ throw 'unable to set null boolean to null'; }");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestIntProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.IntField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.IntField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set IntField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.IntField={0};", (long)int.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set IntField: invalid type: Value is a number, but is too large for a Int32");
            }
            try
            {
                eng.Execute(_content + "mdl.IntField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set IntField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullIntProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullIntField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullIntField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullIntField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullIntField={0};", (long)int.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullIntField: invalid type: Value is a number, but is too large for a Int32");
            }
            try
            {
                eng.Execute(_content + "mdl.NullIntField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestUIntProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.UIntField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.UIntField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UIntField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.UIntField={0};", (long)uint.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UIntField: invalid type: Value is a number, but is too large for a UInt32");
            }
            try
            {
                eng.Execute(_content + "mdl.UIntField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set UIntField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullUIntProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullUIntField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullUIntField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullUIntField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullUIntField={0};", (long)uint.MaxValue + 1));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullUIntField: invalid type: Value is a number, but is too large for a UInt32");
            }
            try
            {
                eng.Execute(_content + "mdl.NullUIntField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestLongProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.LongField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.LongField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set LongField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.LongField=BigInt('{0}')+BigInt('1');", long.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set LongField: invalid type: Value is a number, but is too large for a Int64");
            }
            try
            {
                eng.Execute(_content + "mdl.LongField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set LongField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullLongProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullLongField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullLongField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullLongField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullLongField=BigInt('{0}')+BigInt('1');", long.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullLongField: invalid type: Value is a number, but is too large for a Int64");
            }
            try
            {
                eng.Execute(_content + "mdl.NullLongField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestULongProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.ULongField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.ULongField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ULongField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.ULongField=BigInt('{0}')+BigInt('1');", ulong.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ULongField: invalid type: Value is a number, but is too large for a UInt64");
            }
            try
            {
                eng.Execute(_content + "mdl.ULongField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ULongField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullULongProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullULongField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullULongField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullULongField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullULongField=BigInt('{0}')+BigInt('1');", ulong.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullULongField: invalid type: Value is a number, but is too large for a UInt64");
            }
            try
            {
                eng.Execute(_content + "mdl.NullULongField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestFloatProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.FloatField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.FloatField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set FloatField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.FloatField=Number('{0}')+Number('1e38');", float.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set FloatField: invalid type: Value is a number, but is too large for a Single");
            }
            try
            {
                eng.Execute(_content + "mdl.FloatField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set FloatField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullFloatProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullFloatField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullFloatField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullFloatField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullFloatField=Number('{0}')+Number('1e38');", float.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullFloatField: invalid type: Value is a number, but is too large for a Single");
            }
            try
            {
                eng.Execute(_content + "mdl.NullFloatField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestDecimalProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.DecimalField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.DecimalField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DecimalField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.DecimalField=Number('{0}')+Number('1e58');", decimal.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DecimalField: invalid type: Value is a number, but is too large for a Decimal");
            }
            try
            {
                eng.Execute(_content + "mdl.DecimalField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DecimalField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullDecimalProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullDecimalField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullDecimalField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullDecimalField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullDecimalField=Number('{0}')+Number('1e58');", decimal.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullDecimalField: invalid type: Value is a number, but is too large for a Decimal");
            }
            try
            {
                eng.Execute(_content + "mdl.NullDecimalField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestDoubleProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.DoubleField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.DoubleField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DoubleField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.DoubleField=Number('{0}')+Number('1e308');", double.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DoubleField: invalid type: Value is a number, but is too large for a Double");
            }
            try
            {
                eng.Execute(_content + "mdl.DoubleField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DoubleField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullDoubleProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullDoubleField=10;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullDoubleField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullDoubleField: invalid type: Value not a number and cannot be converted");
            }
            try
            {
                eng.Execute(_content + string.Format("mdl.NullDoubleField=Number('{0}')+Number('1e308');", double.MaxValue));
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullDoubleField: invalid type: Value is a number, but is too large for a Double");
            }
            try
            {
                eng.Execute(_content + "mdl.NullDoubleField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestEnumProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.EnumField='Test1';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.EnumField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set EnumField: invalid type: Value is not in the list of enumarators");
            }
            try
            {
                eng.Execute(_content + @"mdl.EnumField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set EnumField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullEnumProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullEnumField=false;");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullEnumField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullEnumField: invalid type: Value is not in the list of enumarators");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullEnumField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestDateTimeProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.DateTimeField=new Date();");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.DateTimeField='2022-09-01';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.DateTimeField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DateTimeField: invalid type: Value is not a Date and cannot be converted to one");
            }
            try
            {
                eng.Execute(_content + @"mdl.DateTimeField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set DateTimeField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullDateTimeProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullDateTimeField=new Date();");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullDateTimeField='2022-09-01';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullDateTimeField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullDateTimeField: invalid type: Value is not a Date and cannot be converted to one");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullDateTimeField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestByteArrayProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.ByteArrayField='"+Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123"))+"'");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.ByteArrayField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one");
            }
            try
            {
                eng.Execute(_content + @"mdl.ByteArrayField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ByteArrayField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullByteArrayProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullByteArrayField='" + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123")) + "'");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullByteArrayField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullByteArrayField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestIPAddressProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + String.Format("mdl.IPAddressField='{0}';",IPAddress.Loopback));
                Assert.IsTrue(true);
                eng.Execute(_content + String.Format("mdl.IPAddressField='{0}';", IPAddress.IPv6Loopback));
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.IPAddressField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set IPAddressField: invalid type: Value is not an IPAddress");
            }
            try
            {
                eng.Execute(_content + @"mdl.IPAddressField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set IPAddressField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullIPAddressProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + String.Format("mdl.NullIPAddressField='{0}';", IPAddress.Loopback));
                Assert.IsTrue(true);
                eng.Execute(_content + String.Format("mdl.NullIPAddressField='{0}';", IPAddress.IPv6Loopback));
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullIPAddressField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullIPAddressField: invalid type: Value is not an IPAddress");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullIPAddressField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestVersionProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.VersionField='0.0.0';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.VersionField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set VersionField: invalid type: Value is not a Version");
            }
            try
            {
                eng.Execute(_content + @"mdl.VersionField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set VersionField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullVersionProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullVersionField='0.0.0';");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullVersionField='testing';");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullVersionField: invalid type: Value is not a Version");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullVersionField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void TestExceptionProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.ExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';");
                Assert.IsTrue(true);
                eng.Execute(_content + @"try{
    throw 'Testing';
}catch(err){
    mdl.ExceptionField=err;
}");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.ExceptionField={};");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ExceptionField: invalid type: Value is not an Exception");
            }
            try
            {
                eng.Execute(_content + @"mdl.ExceptionField=null;");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set ExceptionField: invalid type: Value is not allowed to be null");
            }
        }

        [TestMethod]
        public void TestNullExceptionProperty()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_content + "mdl.NullExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';");
                Assert.IsTrue(true);
                eng.Execute(_content + @"try{
    throw 'Testing';
}catch(err){
    mdl.NullExceptionField=err;
}");
                Assert.IsTrue(true);
                eng.Execute(_content + "mdl.NullExceptionField={};");
                Assert.IsTrue(false);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.Message, "Cannot set NullExceptionField: invalid type: Value is not an Exception");
            }
            try
            {
                eng.Execute(_content + @"mdl.NullExceptionField=null;");
                Assert.IsTrue(true);
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}
