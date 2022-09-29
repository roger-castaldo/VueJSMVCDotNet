using Jint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.Reddragonit.VueJSMVCDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace AutomatedTesting
{
    [TestClass]
    public class MethodStrongTyping
    {
        private static string _content;

        [TestInitialize]
        public void Init()
        {
            RequestHandler handler = new RequestHandler(RequestHandler.StartTypes.DisableInvalidModels, null);
            int status;
            _content = Constants.JAVASCRIPT_BASE + new StreamReader(Utility.ExecuteRequest("GET","/resources/scripts/mDataTypes.js", handler,out status)).ReadToEnd() + @"

var mdl = App.Models.mDataTypes.createInstance();
";
        }

        [TestCleanup]
        public void Cleanup()
        {
            _content = null;
        }

        private static string _GenerateCalls(string call,bool ignoreBytes)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_content);
            sb.AppendLine(@"var args = [];
while(args.length<=37){
    args.push(null);
}

testArgs = [];
testArgs.push({name:'stringArg',values:[{},null,'testing']});
testArgs.push({name:'nullStringArg',values:[{},null]});
testArgs.push({name:'shortArg',values:['AB',null,32768,10]});
testArgs.push({name:'nullShortArg',values:['AB',32768,null]});
testArgs.push({name:'ushortArg',values:['AB',null,-1,65535,10]});
testArgs.push({name:'nullUShortArg',values:['AB',-1,65535,null]});
testArgs.push({name:'intArg',values:['AB',null,2147483648,10]});
testArgs.push({name:'nullIntArg',values:['AB',2147483648,null]});
testArgs.push({name:'uintArg',values:['AB',null,-1,4294967296,10]});
testArgs.push({name:'nullUIntArg',values:['AB',-1,4294967296,null]});
testArgs.push({name:'longArg',values:['AB',null,BigInt('9223372036854775808'),10]});
testArgs.push({name:'nullLongArg',values:['AB',BigInt('9223372036854775808'),null]});
testArgs.push({name:'ulongArg',values:['AB',null,-1,BigInt('18446744073709551616'),10]});
testArgs.push({name:'nullULongArg',values:['AB',-1,BigInt('18446744073709551616'),null]});
testArgs.push({name:'floatArg',values:['AB',null,Number('3.502823e38'),10]});
testArgs.push({name:'nullFloatArg',values:['AB',Number('3.502823e38'),null]});
testArgs.push({name:'decimalArg',values:['AB',null,Number('79228162514264337593543950336'),10]});
testArgs.push({name:'nullDecimalArg',values:['AB',Number('79228162514264337593543950336'),null]});
testArgs.push({name:'doubleArg',values:['AB',null,Number('1.7976931348623157E+310'),10]});
testArgs.push({name:'nullDoubleArg',values:['AB',Number('1.7976931348623157E+310'),null]});
testArgs.push({name:'byteArg',values:['AB',null,256,10]});
testArgs.push({name:'nullByteArg',values:['AB',256,null]});
testArgs.push({name:'boolArg',values:['AB',null,'testing',false]});
testArgs.push({name:'nullBoolArg',values:['AB','testing',null]});
testArgs.push({name:'enumArg',values:['AB',null,'Test1']});
testArgs.push({name:'nullEnumArg',values:['AB','testing',null]});
testArgs.push({name:'DateTimeArg',values:['AB',null,new Date()]});
testArgs.push({name:'nullDateTimeArg',values:['AB',null]});");
            if (!ignoreBytes)
                sb.AppendLine(@"testArgs.push({name:'byteArrayArg',values:['AB',null,'dGVzdGluZwo=']});
testArgs.push({name:'nullByteArrayArg',values:['AB',null]});");
            sb.AppendLine(@"testArgs.push({name:'IPAddressArg',values:['AB',null,'::1','127.0.0.1']});
testArgs.push({name:'nullIPAddressArg',values:['AB',null]});
testArgs.push({name:'VersionArg',values:['AB',null,'1.0.0']});
testArgs.push({name:'nullVersionArg',values:['AB',null]});
testArgs.push({name:'ExceptionArg',values:[{},null,'error']});
testArgs.push({name:'nullExceptionArg',values:[{},null]});

for(var x=0;x<testArgs.length;x++){
    for(var y=0;y<testArgs[x].values;y++){
        args[x]=testArgs[x].values[y];
        try{");
            sb.AppendLine(string.Format("{0}.apply(mdl,args);", call));
            sb.AppendLine(@"
        }catch(err){
            if (err.indexOf('Cannot set '+testArgs[x].name)<0
                || err.indexOf('invalid type:')<0){
                throw 'failed on '+testArgs[x].name+': '+err;
            }
        }
    }
}");

            return sb.ToString();
        }

        [TestMethod]
        public void TestInstanceMethod()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_GenerateCalls("mdl.TestInputs",false));
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
        public void TestStaticMethod()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_GenerateCalls("App.Models.mDataType.StaticTestInputs",false));
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
        public void TestListMethod()
        {
            Engine eng = new Engine();
            try
            {
                eng.Execute(_GenerateCalls("App.Models.mDataType.TestListInputs",true));
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
