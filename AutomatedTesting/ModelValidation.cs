using AutomatedTesting.Models.InvalidModels;
using AutomatedTesting.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VueJSMVCDotNet;
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

        [TestMethod]
        public void TestModelWithDuplicateMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateLoadMethodException)
                .Select(e => (DuplicateLoadMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethods) && e.MethodName=="DuplicateLoadMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateLoadAllMethodException)
                .Select(e => (DuplicateLoadAllMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethods) && e.MethodName=="DuplicateLoadAllMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelSaveMethodException)
                .Select(e => (DuplicateModelSaveMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethods) && e.MethodName=="DuplicateSaveMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelDeleteMethodException)
                .Select(e => (DuplicateModelDeleteMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethods) && e.MethodName=="DuplicateDeleteMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelUpdateMethodException)
                .Select(e => (DuplicateModelUpdateMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethods) && e.MethodName=="DuplicateUpdateMethod"));
        }

        [TestMethod]
        public void TestModelWithInvalidDataActionMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelSaveMethodException)
                .Select(e => (InvalidModelSaveMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="InvalidSave"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelDeleteMethodException)
                .Select(e => (InvalidModelDeleteMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="InvalidDelete"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelUpdateMethodException)
                .Select(e => (InvalidModelUpdateMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="InvalidUpdate"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadMethodArguements)
                .Select(e => (InvalidLoadMethodArguements)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="NoArgumentLoad"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadMethodArguements)
                .Select(e => (InvalidLoadMethodArguements)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="NotStringLoad"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadMethodReturnType)
                .Select(e => (InvalidLoadMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="InvalidReturnLoad"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="NotArrayReturnAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="WrongArrayTypeLoadAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="WrongListTypeLoadAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllArguements)
                .Select(e => (InvalidLoadAllArguements)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethods) && e.MethodName=="LoadAllWithInvalidArguements"));
        }

        [TestMethod]
        public void TestModelMissingEmptyConstructorForSave()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is NoEmptyConstructorException)
                .Select(e => (NoEmptyConstructorException)e)
                .Count(e => e.ModelType==typeof(ModelMissingEmptyConstructorForSave)));
        }

        [TestMethod]
        public void TestModelWithDuplicateRoute()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateRouteException)
                .Select(e => (DuplicateRouteException)e)
                .Count(e => (e.FirstModel==typeof(ModelWithDuplicateRoute) && e.FirstPath=="*/models/ModelWithDuplicateMethods" && e.SecondModel==typeof(ModelWithDuplicateMethods) && e.SecondPath=="*/models/ModelWithDuplicateMethods")
                || (e.FirstModel==typeof(ModelWithDuplicateMethods) && e.FirstPath=="*/models/ModelWithDuplicateMethods" && e.SecondModel==typeof(ModelWithDuplicateRoute) && e.SecondPath=="*/models/ModelWithDuplicateMethods")));
        }

        [TestMethod]
        public void TestModelWithInvalidListMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            //nullable invalid return
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListMethodReturnException)
                .Select(e => (InvalidModelListMethodReturnException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="SearchNullable"));

            //array invalid return
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListMethodReturnException)
                .Select(e => (InvalidModelListMethodReturnException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="SearchArray"));

            //missing paging parameters
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListParameterCountException)
                .Select(e => (InvalidModelListParameterCountException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="InvalidPagedSignature"));

            //out parameter
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListParameterOutException)
                .Select(e => (InvalidModelListParameterOutException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="WithOutParameter" && e.Parameter.Name=="par1"));

            //invalid paged parameter type
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListPageParameterTypeException)
                .Select(e => (InvalidModelListPageParameterTypeException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="PagedInvalidParameterType" && e.Parameter.Name=="pageStartIndex"));

            //invalid total pages parameter
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListPageTotalPagesNotOutException)
                .Select(e => (InvalidModelListPageTotalPagesNotOutException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethods) && e.MethodName=="PagedInvalidOutParameter" && e.Parameter.Name=="pageSize"));
        }

        [TestMethod]
        public void TestModelWithBlockedID()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is ModelIDBlockedException)
                .Select(e => (ModelIDBlockedException)e)
                .Count(e => e.ModelType==typeof(ModelWithBlockedID)));
        }

        [TestMethod]
        public void TestModelWithNoLoad()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is NoLoadMethodException)
                .Select(e => (NoLoadMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithNoLoad)));
        }

        [TestMethod]
        public void TestModelWithInvalidExposedMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateMethodSignatureException)
                .Select(e => (DuplicateMethodSignatureException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethods) && e.MethodName=="DuplicateExposedStaticMethod"));

            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is MethodNotMarkedAsSlow)
                .Select(e => (MethodNotMarkedAsSlow)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethods) && e.MethodName=="NotSlowWithAddItem"));

            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is MethodWithAddItemNotVoid)
                .Select(e => (MethodWithAddItemNotVoid)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethods) && e.MethodName=="SlowWithAddItemAndReturn"));
        }
    }
}
