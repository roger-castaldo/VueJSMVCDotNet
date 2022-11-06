using AutomatedTesting.Security;
using Jint;
using Microsoft.AspNetCore.Http;
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
            VueMiddleware middleware = Utility.CreateMiddleware(true);
            int status;
            _content = new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mDataTypes.js", middleware,out status)).ReadToEnd();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _content = null;
        }

        private void _ExecuteTest(string additionalCode, string errorMessage)
        {
            Engine eng = Utility.CreateEngine();
            try
            {
                eng.Execute(Constants.JAVASCRIPT_BASE);
                eng.AddModule("mDataTypes", _content);
                eng.AddModule("custom", string.Format(@"
    import {{ mDataTypes }} from 'mDataTypes';
    let mdl = new mDataTypes();
    {0}
    export const name = 'John';
", additionalCode));
                var ns = eng.ImportModule("custom");
                Assert.IsTrue((errorMessage!=null ? false : ns.Get("name").AsString()=="John"));
            }
            catch (Esprima.ParserException e)
            {
                Assert.Fail(e.Message);
            }
            catch (Exception e)
            {
                if (errorMessage!=null)
                    Assert.AreEqual(e.Message, errorMessage);
                else
                    Assert.Fail(e.Message);
            }
            finally
            {
                eng.Dispose();
            }
        }

        [TestMethod]
        public void TestStringProperty()
        {
            _ExecuteTest("mdl.StringField='A string';", null);
            _ExecuteTest("mdl.StringField={};", "Cannot set StringField: invalid type: Value not a string and cannot be converted");
            _ExecuteTest("mdl.StringField=null;", "Cannot set StringField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullStringProperty()
        {
            _ExecuteTest("mdl.NullStringField='A string';", null);
            _ExecuteTest("mdl.NullStringField={};", "Cannot set NullStringField: invalid type: Value not a string and cannot be converted");
            _ExecuteTest("mdl.NullStringField=null;", null);
        }

        [TestMethod]
        public void TestCharProperty()
        {
            _ExecuteTest("mdl.CharField='A';", null);
            _ExecuteTest("mdl.CharField='AB';", "Cannot set CharField: invalid type: Value not a char");
            _ExecuteTest("mdl.CharField=null;", "Cannot set CharField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullCharProperty()
        {
            _ExecuteTest("mdl.NullCharField='A';", null);
            _ExecuteTest("mdl.NullCharField='AB';", "Cannot set NullCharField: invalid type: Value not a char");
            _ExecuteTest("mdl.NullCharField=null;", null);
        }

        [TestMethod]
        public void TestShortProperty()
        {
            _ExecuteTest("mdl.ShortField=10;", null);
            _ExecuteTest("mdl.ShortField='testing';", "Cannot set ShortField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.ShortField={0};", (int)short.MaxValue + 1), "Cannot set ShortField: invalid type: Value is a number, but is too large for a Int16");
            _ExecuteTest("mdl.ShortField=null;", "Cannot set ShortField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullShortProperty()
        {
            _ExecuteTest("mdl.NullShortField=10;", null);
            _ExecuteTest("mdl.NullShortField='testing';", "Cannot set NullShortField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullShortField={0};", (int)short.MaxValue + 1), "Cannot set NullShortField: invalid type: Value is a number, but is too large for a Int16");
            _ExecuteTest("mdl.NullShortField=null;", null);
        }

        [TestMethod]
        public void TestUShortProperty()
        {
            _ExecuteTest("mdl.UShortField=10;", null);
            _ExecuteTest("mdl.UShortField='testing';", "Cannot set UShortField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.UShortField={0};", (int)ushort.MaxValue + 1), "Cannot set UShortField: invalid type: Value is a number, but is too large for a UInt16");
            _ExecuteTest("mdl.UShortField=null;", "Cannot set UShortField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullUShortProperty()
        {
            _ExecuteTest("mdl.NullUShortField=10;", null);
            _ExecuteTest("mdl.NullUShortField='testing';", "Cannot set NullUShortField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullUShortField={0};", (int)ushort.MaxValue + 1), "Cannot set NullUShortField: invalid type: Value is a number, but is too large for a UInt16");
            _ExecuteTest("mdl.NullUShortField=null;", null);
        }

        [TestMethod]
        public void TestByteProperty()
        {
            _ExecuteTest("mdl.ByteField=10;", null);
            _ExecuteTest("mdl.ByteField='testing';", "Cannot set ByteField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.ByteField={0};", (int)byte.MaxValue + 1), "Cannot set ByteField: invalid type: Value is a number, but is too large for a Byte");
            _ExecuteTest("mdl.ByteField=null;", "Cannot set ByteField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullByteProperty()
        {
            _ExecuteTest("mdl.NullByteField=10;", null);
            _ExecuteTest("mdl.NullByteField='testing';", "Cannot set NullByteField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullByteField={0};", (int)byte.MaxValue + 1), "Cannot set NullByteField: invalid type: Value is a number, but is too large for a Byte");
            _ExecuteTest("mdl.NullByteField=null;", null);
        }

        [TestMethod]
        public void TestBooleanProperty()
        {
            _ExecuteTest("mdl.BooleanField=false;", null);
            _ExecuteTest("mdl.BooleanField='testing';", "Cannot set BooleanField: invalid type: Value not boolean and cannot be converted");
            _ExecuteTest(@"mdl.BooleanField=null;
if (mdl.BooleanField!==false){ throw 'unable to set null boolean to convert to false'; }", null);
        }

        [TestMethod]
        public void TestNullBooleanProperty()
        {
            _ExecuteTest("mdl.NullBooleanField=false;", null);
            _ExecuteTest("mdl.NullBooleanField='testing';", "Cannot set NullBooleanField: invalid type: Value not boolean and cannot be converted");
            _ExecuteTest(@"mdl.NullBooleanField=null;
if (mdl.NullBooleanField!==null){ throw 'unable to set null boolean to null'; }", null);
        }

        [TestMethod]
        public void TestIntProperty()
        {
            _ExecuteTest("mdl.IntField=10;", null);
            _ExecuteTest("mdl.IntField='testing';", "Cannot set IntField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.IntField={0};", (long)int.MaxValue + 1), "Cannot set IntField: invalid type: Value is a number, but is too large for a Int32");
            _ExecuteTest("mdl.IntField=null;", "Cannot set IntField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullIntProperty()
        {
            _ExecuteTest("mdl.NullIntField=10;", null);
            _ExecuteTest("mdl.NullIntField='testing';", "Cannot set NullIntField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullIntField={0};", (long)int.MaxValue + 1), "Cannot set NullIntField: invalid type: Value is a number, but is too large for a Int32");
            _ExecuteTest("mdl.NullIntField=null;", null);
        }

        [TestMethod]
        public void TestUIntProperty()
        {
            _ExecuteTest("mdl.UIntField=10;", null);
            _ExecuteTest("mdl.UIntField='testing';", "Cannot set UIntField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.UIntField={0};", (long)uint.MaxValue + 1), "Cannot set UIntField: invalid type: Value is a number, but is too large for a UInt32");
            _ExecuteTest("mdl.UIntField=null;", "Cannot set UIntField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullUIntProperty()
        {
            _ExecuteTest("mdl.NullUIntField=10;", null);
            _ExecuteTest("mdl.NullUIntField='testing';", "Cannot set NullUIntField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullUIntField={0};", (long)uint.MaxValue + 1), "Cannot set NullUIntField: invalid type: Value is a number, but is too large for a UInt32");
            _ExecuteTest("mdl.NullUIntField=null;", null);
        }

        [TestMethod]
        public void TestLongProperty()
        {
            _ExecuteTest("mdl.LongField=10;", null);
            _ExecuteTest("mdl.LongField='testing';", "Cannot set LongField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.LongField=BigInt('{0}')+BigInt('1');", long.MaxValue), "Cannot set LongField: invalid type: Value is a number, but is too large for a Int64");
            _ExecuteTest("mdl.LongField=null;", "Cannot set LongField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullLongProperty()
        {
            _ExecuteTest("mdl.NullLongField=10;", null);
            _ExecuteTest("mdl.NullLongField='testing';", "Cannot set NullLongField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullLongField=BigInt('{0}')+BigInt('1');", long.MaxValue), "Cannot set NullLongField: invalid type: Value is a number, but is too large for a Int64");
            _ExecuteTest("mdl.NullLongField=null;", null);
        }

        [TestMethod]
        public void TestULongProperty()
        {
            _ExecuteTest("mdl.ULongField=10;", null);
            _ExecuteTest("mdl.ULongField='testing';", "Cannot set ULongField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.ULongField=BigInt('{0}')+BigInt('1');", ulong.MaxValue), "Cannot set ULongField: invalid type: Value is a number, but is too large for a UInt64");
            _ExecuteTest("mdl.ULongField=null;", "Cannot set ULongField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullULongProperty()
        {
            _ExecuteTest("mdl.NullULongField=10;", null);
            _ExecuteTest("mdl.NullULongField='testing';", "Cannot set NullULongField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullULongField=BigInt('{0}')+BigInt('1');", ulong.MaxValue), "Cannot set NullULongField: invalid type: Value is a number, but is too large for a UInt64");
            _ExecuteTest("mdl.NullULongField=null;", null);
        }

        [TestMethod]
        public void TestFloatProperty()
        {
            _ExecuteTest("mdl.FloatField=10;", null);
            _ExecuteTest("mdl.FloatField='testing';", "Cannot set FloatField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.FloatField=Number('{0}')+Number('1e38');", float.MaxValue), "Cannot set FloatField: invalid type: Value is a number, but is too large for a Single");
            _ExecuteTest("mdl.FloatField=null;", "Cannot set FloatField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullFloatProperty()
        {
            _ExecuteTest("mdl.NullFloatField=10;", null);
            _ExecuteTest("mdl.NullFloatField='testing';", "Cannot set NullFloatField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullFloatField=Number('{0}')+Number('1e38');", float.MaxValue), "Cannot set NullFloatField: invalid type: Value is a number, but is too large for a Single");
            _ExecuteTest("mdl.NullFloatField=null;", null);
        }

        [TestMethod]
        public void TestDecimalProperty()
        {
            _ExecuteTest("mdl.DecimalField=10;", null);
            _ExecuteTest("mdl.DecimalField='testing';", "Cannot set DecimalField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.DecimalField=Number('{0}')+Number('1e58');", decimal.MaxValue), "Cannot set DecimalField: invalid type: Value is a number, but is too large for a Decimal");
            _ExecuteTest("mdl.DecimalField=null;", "Cannot set DecimalField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullDecimalProperty()
        {
            _ExecuteTest("mdl.NullDecimalField=10;", null);
            _ExecuteTest("mdl.NullDecimalField='testing';", "Cannot set NullDecimalField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullDecimalField=Number('{0}')+Number('1e58');", decimal.MaxValue), "Cannot set NullDecimalField: invalid type: Value is a number, but is too large for a Decimal");
            _ExecuteTest("mdl.NullDecimalField=null;", null);
        }

        [TestMethod]
        public void TestDoubleProperty()
        {
            _ExecuteTest("mdl.DoubleField=10;", null);
            _ExecuteTest("mdl.DoubleField='testing';", "Cannot set DoubleField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.DoubleField=Number('{0}')+Number('1e308');", double.MaxValue), "Cannot set DoubleField: invalid type: Value is a number, but is too large for a Double");
            _ExecuteTest("mdl.DoubleField=null;", "Cannot set DoubleField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullDoubleProperty()
        {
            _ExecuteTest("mdl.NullDoubleField=10;", null);
            _ExecuteTest("mdl.NullDoubleField='testing';", "Cannot set NullDoubleField: invalid type: Value not a number and cannot be converted");
            _ExecuteTest(string.Format("mdl.NullDoubleField=Number('{0}')+Number('1e308');", double.MaxValue), "Cannot set NullDoubleField: invalid type: Value is a number, but is too large for a Double");
            _ExecuteTest("mdl.NullDoubleField=null;", null);
        }

        [TestMethod]
        public void TestEnumProperty()
        {
            _ExecuteTest("mdl.EnumField='Test1';", null);
            _ExecuteTest("mdl.EnumField='testing';", "Cannot set EnumField: invalid type: Value is not in the list of enumarators");
            _ExecuteTest("mdl.EnumField=null;", "Cannot set EnumField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullEnumProperty()
        {
            _ExecuteTest("mdl.NullEnumField='Test1';", null);
            _ExecuteTest("mdl.NullEnumField='testing';", "Cannot set NullEnumField: invalid type: Value is not in the list of enumarators");
            _ExecuteTest("mdl.NullEnumField=null;", null);
        }

        [TestMethod]
        public void TestDateTimeProperty()
        {
            _ExecuteTest("mdl.DateTimeField=new Date();", null);
            _ExecuteTest("mdl.DateTimeField='2022-09-01';", null);
            _ExecuteTest("mdl.DateTimeField='testing';", "Cannot set DateTimeField: invalid type: Value is not a Date and cannot be converted to one");
            _ExecuteTest("mdl.DateTimeField=null;", "Cannot set DateTimeField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullDateTimeProperty()
        {
            _ExecuteTest("mdl.NullDateTimeField=new Date();", null);
            _ExecuteTest("mdl.NullDateTimeField='2022-09-01';", null);
            _ExecuteTest("mdl.NullDateTimeField='testing';", "Cannot set NullDateTimeField: invalid type: Value is not a Date and cannot be converted to one");
            _ExecuteTest("mdl.NullDateTimeField=null;", null);
        }

        [TestMethod]
        public void TestByteArrayProperty()
        {
            _ExecuteTest("mdl.ByteArrayField='"+Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123"))+"'", null);
            _ExecuteTest("mdl.ByteArrayField='testing';", "Cannot set ByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one");
            _ExecuteTest("mdl.ByteArrayField=null;", "Cannot set ByteArrayField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullByteArrayProperty()
        {
            _ExecuteTest("mdl.NullByteArrayField='"+Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123"))+"'", null);
            _ExecuteTest("mdl.NullByteArrayField='testing';", "Cannot set NullByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one");
            _ExecuteTest("mdl.NullByteArrayField=null;", null);
        }

        [TestMethod]
        public void TestIPAddressProperty()
        {
            _ExecuteTest(String.Format("mdl.IPAddressField='{0}';", IPAddress.Loopback), null);
            _ExecuteTest(String.Format("mdl.IPAddressField='{0}';", IPAddress.IPv6Loopback), null);
            _ExecuteTest("mdl.IPAddressField='testing';", "Cannot set IPAddressField: invalid type: Value is not an IPAddress");
            _ExecuteTest("mdl.IPAddressField=null;", "Cannot set IPAddressField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullIPAddressProperty()
        {
            _ExecuteTest(String.Format("mdl.NullIPAddressField='{0}';", IPAddress.Loopback), null);
            _ExecuteTest(String.Format("mdl.NullIPAddressField='{0}';", IPAddress.IPv6Loopback), null);
            _ExecuteTest("mdl.NullIPAddressField='testing';", "Cannot set NullIPAddressField: invalid type: Value is not an IPAddress");
            _ExecuteTest("mdl.NullIPAddressField=null;", null);
        }

        [TestMethod]
        public void TestVersionProperty()
        {
            _ExecuteTest("mdl.VersionField='0.0.0';", null);
            _ExecuteTest("mdl.VersionField='testing';", "Cannot set VersionField: invalid type: Value is not a Version");
            _ExecuteTest("mdl.VersionField=null;", "Cannot set VersionField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullVersionProperty()
        {
            _ExecuteTest("mdl.NullVersionField='0.0.0';", null);
            _ExecuteTest("mdl.NullVersionField='testing';", "Cannot set NullVersionField: invalid type: Value is not a Version");
            _ExecuteTest("mdl.NullVersionField=null;",null);
        }

        [TestMethod]
        public void TestExceptionProperty()
        {
            _ExecuteTest("mdl.ExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';", null);
            _ExecuteTest(@"try{
    throw 'Testing';
}catch(err){
    mdl.ExceptionField=err;
}", null);
            _ExecuteTest("mdl.ExceptionField={};", "Cannot set ExceptionField: invalid type: Value is not an Exception");
            _ExecuteTest("mdl.ExceptionField=null;", "Cannot set ExceptionField: invalid type: Value is not allowed to be null");
        }

        [TestMethod]
        public void TestNullExceptionProperty()
        {
            _ExecuteTest("mdl.NullExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';", null);
            _ExecuteTest(@"try{
    throw 'Testing';
}catch(err){
    mdl.NullExceptionField=err;
}", null);
            _ExecuteTest("mdl.NullExceptionField={};", "Cannot set NullExceptionField: invalid type: Value is not an Exception");
            _ExecuteTest("mdl.NullExceptionField=null;", null);
        }
    }
}
