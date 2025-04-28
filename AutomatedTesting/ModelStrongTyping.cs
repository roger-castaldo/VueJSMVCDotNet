using Jint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class ModelStrongTyping
    {
        [TestMethod]
        [DataRow("mdl.StringField='A string';", null)]
        [DataRow("mdl.StringField={};", "Cannot set StringField: invalid type: Value not a string and cannot be converted")]
        [DataRow("mdl.StringField=null;", "Cannot set StringField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullStringField='A string';", null)]
        [DataRow("mdl.NullStringField={};", "Cannot set NullStringField: invalid type: Value not a string and cannot be converted")]
        [DataRow("mdl.NullStringField=null;", null)]
        [DataRow("mdl.CharField='A';", null)]
        [DataRow("mdl.CharField='AB';", "Cannot set CharField: invalid type: Value not a char")]
        [DataRow("mdl.CharField=null;", "Cannot set CharField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullCharField='A';", null)]
        [DataRow("mdl.NullCharField='AB';", "Cannot set NullCharField: invalid type: Value not a char")]
        [DataRow("mdl.NullCharField=null;", null)]
        [DataRow("mdl.ShortField=10;", null)]
        [DataRow("mdl.ShortField='testing';", "Cannot set ShortField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.ShortField=null;", "Cannot set ShortField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullShortField=10;", null)]
        [DataRow("mdl.NullShortField='testing';", "Cannot set NullShortField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullShortField=null;", null)]
        [DataRow("mdl.UShortField=10;", null)]
        [DataRow("mdl.UShortField='testing';", "Cannot set UShortField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.UShortField=null;", "Cannot set UShortField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullUShortField=10;", null)]
        [DataRow("mdl.NullUShortField='testing';", "Cannot set NullUShortField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullUShortField=null;", null)]
        [DataRow("mdl.ByteField=10;", null)]
        [DataRow("mdl.ByteField='testing';", "Cannot set ByteField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.ByteField=null;", "Cannot set ByteField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullByteField=10;", null)]
        [DataRow("mdl.NullByteField='testing';", "Cannot set NullByteField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullByteField=null;", null)]
        [DataRow("mdl.BooleanField=false;", null)]
        [DataRow("mdl.BooleanField='testing';", "Cannot set BooleanField: invalid type: Value not boolean and cannot be converted")]
        [DataRow(@"mdl.BooleanField=null;
if (mdl.BooleanField!==false){ throw 'unable to set null boolean to convert to false'; }", null)]
        [DataRow("mdl.NullBooleanField=false;", null)]
        [DataRow("mdl.NullBooleanField='testing';", "Cannot set NullBooleanField: invalid type: Value not boolean and cannot be converted")]
        [DataRow(@"mdl.NullBooleanField=null;
if (mdl.NullBooleanField!==null){ throw 'unable to set null boolean to null'; }", null)]
        [DataRow("mdl.IntField=10;", null)]
        [DataRow("mdl.IntField='testing';", "Cannot set IntField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.IntField=null;", "Cannot set IntField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullIntField=10;", null)]
        [DataRow("mdl.NullIntField='testing';", "Cannot set NullIntField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullIntField=null;", null)]
        [DataRow("mdl.UIntField=10;", null)]
        [DataRow("mdl.UIntField='testing';", "Cannot set UIntField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.UIntField=null;", "Cannot set UIntField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullUIntField=10;", null)]
        [DataRow("mdl.NullUIntField='testing';", "Cannot set NullUIntField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullUIntField=null;", null)]
        [DataRow("mdl.LongField=10;", null)]
        [DataRow("mdl.LongField='testing';", "Cannot set LongField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.LongField=null;", "Cannot set LongField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullLongField=10;", null)]
        [DataRow("mdl.NullLongField='testing';", "Cannot set NullLongField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullLongField=null;", null)]
        [DataRow("mdl.ULongField=10;", null)]
        [DataRow("mdl.ULongField='testing';", "Cannot set ULongField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.ULongField=null;", "Cannot set ULongField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullULongField=10;", null)]
        [DataRow("mdl.NullULongField='testing';", "Cannot set NullULongField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullULongField=null;", null)]
        [DataRow("mdl.FloatField=10;", null)]
        [DataRow("mdl.FloatField='testing';", "Cannot set FloatField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.FloatField=null;", "Cannot set FloatField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullFloatField=10;", null)]
        [DataRow("mdl.NullFloatField='testing';", "Cannot set NullFloatField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullFloatField=null;", null)]
        [DataRow("mdl.DecimalField=10;", null)]
        [DataRow("mdl.DecimalField='testing';", "Cannot set DecimalField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.DecimalField=null;", "Cannot set DecimalField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullDecimalField=10;", null)]
        [DataRow("mdl.NullDecimalField='testing';", "Cannot set NullDecimalField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullDecimalField=null;", null)]
        [DataRow("mdl.DoubleField=10;", null)]
        [DataRow("mdl.DoubleField='testing';", "Cannot set DoubleField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.DoubleField=null;", "Cannot set DoubleField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullDoubleField=10;", null)]
        [DataRow("mdl.NullDoubleField='testing';", "Cannot set NullDoubleField: invalid type: Value not a number and cannot be converted")]
        [DataRow("mdl.NullDoubleField=null;", null)]
        [DataRow("mdl.EnumField='Test1';", null)]
        [DataRow("mdl.EnumField='testing';", "Cannot set EnumField: invalid type: Value is not in the list of enumarators")]
        [DataRow("mdl.EnumField=null;", "Cannot set EnumField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullEnumField='Test1';", null)]
        [DataRow("mdl.NullEnumField='testing';", "Cannot set NullEnumField: invalid type: Value is not in the list of enumarators")]
        [DataRow("mdl.NullEnumField=null;", null)]
        [DataRow("mdl.DateTimeField=new Date();", null)]
        [DataRow("mdl.DateTimeField='2022-09-01';", null)]
        [DataRow("mdl.DateTimeField='testing';", "Cannot set DateTimeField: invalid type: Value is not a Date and cannot be converted to one")]
        [DataRow("mdl.DateTimeField=null;", "Cannot set DateTimeField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullDateTimeField=new Date();", null)]
        [DataRow("mdl.NullDateTimeField='2022-09-01';", null)]
        [DataRow("mdl.NullDateTimeField='testing';", "Cannot set NullDateTimeField: invalid type: Value is not a Date and cannot be converted to one")]
        [DataRow("mdl.NullDateTimeField=null;", null)]
        [DataRow("mdl.ByteArrayField='testing';", "Cannot set ByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one")]
        [DataRow("mdl.ByteArrayField=null;", "Cannot set ByteArrayField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullByteArrayField='testing';", "Cannot set NullByteArrayField: invalid type: Value is not a Byte[] and cannot be converted to one")]
        [DataRow("mdl.NullByteArrayField=null;", null)]
        [DataRow("mdl.IPAddressField='testing';", "Cannot set IPAddressField: invalid type: Value is not an IPAddress")]
        [DataRow("mdl.IPAddressField=null;", "Cannot set IPAddressField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullIPAddressField='testing';", "Cannot set NullIPAddressField: invalid type: Value is not an IPAddress")]
        [DataRow("mdl.NullIPAddressField=null;", null)]
        [DataRow("mdl.VersionField='0.0.0';", null)]
        [DataRow("mdl.VersionField='testing';", "Cannot set VersionField: invalid type: Value is not a Version")]
        [DataRow("mdl.VersionField=null;", "Cannot set VersionField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullVersionField='0.0.0';", null)]
        [DataRow("mdl.NullVersionField='testing';", "Cannot set NullVersionField: invalid type: Value is not a Version")]
        [DataRow("mdl.NullVersionField=null;", null)]
        [DataRow("mdl.ExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';", null)]
        [DataRow(@"try{
    throw 'Testing';
}catch(err){
    mdl.ExceptionField=err;
}", null)]
        [DataRow("mdl.ExceptionField={};", "Cannot set ExceptionField: invalid type: Value is not an Exception")]
        [DataRow("mdl.ExceptionField=null;", "Cannot set ExceptionField: invalid type: Value is not allowed to be null")]
        [DataRow("mdl.NullExceptionField='{\"Message\":\"Testing\",\"StackTrace\":\"123\\n456\\n789\",\"Source\":\"Testing.cs\"}';", null)]
        [DataRow(@"try{
    throw 'Testing';
}catch(err){
    mdl.NullExceptionField=err;
}", null)]
        [DataRow("mdl.NullExceptionField={};", "Cannot set NullExceptionField: invalid type: Value is not an Exception")]
        [DataRow("mdl.NullExceptionField=null;", null)]
        public async Task ExecuteTest(string additionalCode, string errorMessage)
        {
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var (responseStream, _, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.DataTypesModelRoute}.js", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            using Engine eng = await Utility.CreateEngineAsync(webApplicationFactory);
            try
            {
                eng.Execute(Constants.JAVASCRIPT_BASE);
                eng.Modules.Add("mDataTypes", content);
                eng.Modules.Add("custom", string.Format(@"
    import {{ mDataTypes }} from 'mDataTypes';
    let mdl = new mDataTypes();
    {0}
    export const name = 'John';
", additionalCode));
                var ns = eng.Modules.Import("custom");
                Assert.IsTrue((errorMessage!=null ? false : ns.Get("name").AsString()=="John"));
            }
            catch (Exception e)
            {
                if (errorMessage!=null)
                    Assert.AreEqual(e.Message, errorMessage);
                else
                    Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public async Task ExecuteSpecialCases()
        {
            await ExecuteTest($"mdl.ShortField={short.MaxValue}+1;", "Cannot set ShortField: invalid type: Value is a number, but is too large for a Int16");
            await ExecuteTest($"mdl.NullShortField={short.MaxValue}+1;", "Cannot set NullShortField: invalid type: Value is a number, but is too large for a Int16");
            await ExecuteTest($"mdl.UShortField={ushort.MaxValue}+1;", "Cannot set UShortField: invalid type: Value is a number, but is too large for a UInt16");
            await ExecuteTest($"mdl.NullUShortField={ushort.MaxValue}+1;", "Cannot set NullUShortField: invalid type: Value is a number, but is too large for a UInt16");
            await ExecuteTest($"mdl.ByteField={byte.MaxValue}+1;", "Cannot set ByteField: invalid type: Value is a number, but is too large for a Byte");
            await ExecuteTest($"mdl.NullByteField={byte.MaxValue}+1;", "Cannot set NullByteField: invalid type: Value is a number, but is too large for a Byte");
            await ExecuteTest($"mdl.IntField={int.MaxValue}+1;", "Cannot set IntField: invalid type: Value is a number, but is too large for a Int32");
            await ExecuteTest($"mdl.NullIntField={int.MaxValue}+1;", "Cannot set NullIntField: invalid type: Value is a number, but is too large for a Int32");
            await ExecuteTest($"mdl.UIntField={uint.MaxValue}+1;", "Cannot set UIntField: invalid type: Value is a number, but is too large for a UInt32");
            await ExecuteTest($"mdl.NullUIntField={uint.MaxValue}+1;", "Cannot set NullUIntField: invalid type: Value is a number, but is too large for a UInt32");
            await ExecuteTest($"mdl.LongField=BigInt('{long.MaxValue}')+BigInt('1');", "Cannot set LongField: invalid type: Value is a number, but is too large for a Int64");
            await ExecuteTest($"mdl.NullLongField=BigInt('{long.MaxValue}')+BigInt('1');", "Cannot set NullLongField: invalid type: Value is a number, but is too large for a Int64");
            await ExecuteTest($"mdl.ULongField=BigInt('{ulong.MaxValue}')+BigInt('1');", "Cannot set ULongField: invalid type: Value is a number, but is too large for a UInt64");
            await ExecuteTest($"mdl.NullULongField=BigInt('{ulong.MaxValue}')+BigInt('1');", "Cannot set NullULongField: invalid type: Value is a number, but is too large for a UInt64");
            await ExecuteTest($"mdl.FloatField=Number('{float.MaxValue}')+Number('1e38');", "Cannot set FloatField: invalid type: Value is a number, but is too large for a Single");
            await ExecuteTest($"mdl.NullFloatField=Number('{float.MaxValue}')+Number('1e38');", "Cannot set NullFloatField: invalid type: Value is a number, but is too large for a Single");
            await ExecuteTest($"mdl.DecimalField=Number('{decimal.MaxValue}')+Number('1e58');", "Cannot set DecimalField: invalid type: Value is a number, but is too large for a Decimal");
            await ExecuteTest($"mdl.NullDecimalField=Number('{decimal.MaxValue}')+Number('1e58');", "Cannot set NullDecimalField: invalid type: Value is a number, but is too large for a Decimal");
            await ExecuteTest($"mdl.DoubleField=Number('{double.MaxValue}')+Number('1e308');", "Cannot set DoubleField: invalid type: Value is a number, but is too large for a Double");
            await ExecuteTest($"mdl.NullDoubleField=Number('{double.MaxValue}')+Number('1e308');", "Cannot set NullDoubleField: invalid type: Value is a number, but is too large for a Double");
            await ExecuteTest($"mdl.ByteArrayField='{Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123"))}'", null);
            await ExecuteTest($"mdl.NullByteArrayField='{Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes("Testing123"))}'", null);
            await ExecuteTest($"mdl.IPAddressField='{IPAddress.Loopback}';", null);
            await ExecuteTest($"mdl.IPAddressField='{IPAddress.IPv6Loopback}';", null);
            await ExecuteTest($"mdl.NullIPAddressField='{IPAddress.Loopback}';", null);
            await ExecuteTest($"mdl.NullIPAddressField='{IPAddress.IPv6Loopback}';", null);
        }
    }
}
