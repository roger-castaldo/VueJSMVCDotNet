using AutomatedTesting.Handlers.InvalidModels;
using AutomatedTesting.Models.InvalidModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VueJSMVCDotNet;

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
                (var webApplicationFactory, _, _) = Utility.CreateApplication(false);
                _ = webApplicationFactory.CreateClient();
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
        public async Task TestDisableInvalid()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (_, failedResponseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, "/resources/scripts/mInvalidModel.js", webApplicationFactory);
            var (_, successResponseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}.js", webApplicationFactory);

            //Assert
            Assert.AreEqual(404, failedResponseStatus);
            Assert.AreEqual(200, successResponseStatus);
        }

        [TestMethod]
        public void TestModelWithNoRoute()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions.Count(e => e is NoRouteException && ((NoRouteException)e).ModelType==typeof(ModelWithNoRouteHandler)));
        }

        [TestMethod]
        public void TestModelWithDuplicateMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateLoadAllMethodException)
                .Select(e => (DuplicateLoadAllMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethodsHandler) && e.MethodName=="DuplicateLoadAllMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelSaveMethodException)
                .Select(e => (DuplicateModelSaveMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethodsHandler) && e.MethodName=="DuplicateSaveMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelDeleteMethodException)
                .Select(e => (DuplicateModelDeleteMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethodsHandler) && e.MethodName=="DuplicateDeleteMethod"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is DuplicateModelUpdateMethodException)
                .Select(e => (DuplicateModelUpdateMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithDuplicateMethodsHandler) && e.MethodName=="DuplicateUpdateMethod"));
        }

        [TestMethod]
        public void TestModelWithInvalidDataActionMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelSaveMethodException)
                .Select(e => (InvalidModelSaveMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="InvalidSave"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelDeleteMethodException)
                .Select(e => (InvalidModelDeleteMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="InvalidDelete"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelUpdateMethodException)
                .Select(e => (InvalidModelUpdateMethodException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="InvalidUpdate"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="NotArrayReturnAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="WrongArrayTypeLoadAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllMethodReturnType)
                .Select(e => (InvalidLoadAllMethodReturnType)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="WrongListTypeLoadAll"));
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidLoadAllArguements)
                .Select(e => (InvalidLoadAllArguements)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidDataActionMethodsHandler) && e.MethodName=="LoadAllWithInvalidArguements"));
        }

        [TestMethod]
        public void TestModelWithDuplicateRoute()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(2, ex.InnerExceptions
                .Where(e => e is DuplicateRouteException)
                .Select(e => (DuplicateRouteException)e)
                .Count(e => (e.FirstModel==typeof(ModelWithDuplicateRouteHandler) && e.FirstPath=="/models/ModelWithDuplicateMethods" && e.SecondModel==typeof(ModelWithDuplicateMethodsHandler) && e.SecondPath=="/models/ModelWithDuplicateMethods")
                || (e.FirstModel==typeof(ModelWithDuplicateMethodsHandler) && e.FirstPath=="/models/ModelWithDuplicateMethods" && e.SecondModel==typeof(ModelWithDuplicateRouteHandler) && e.SecondPath=="/models/ModelWithDuplicateMethods")));
        }

        [TestMethod]
        public void TestModelWithInvalidListMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            //nullable invalid return
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListMethodReturnException)
                .Select(e => (InvalidModelListMethodReturnException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethodsHandler) && e.MethodName=="SearchNullable"));

            //array invalid return
            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is InvalidModelListMethodReturnException)
                .Select(e => (InvalidModelListMethodReturnException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidListMethodsHandler) && e.MethodName=="SearchArray"));
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
        public void TestModelWithInvalidExposedMethods()
        {
            ModelValidationException ex = _LoadExceptions();
            Assert.AreEqual(2, ex.InnerExceptions
                .Where(e => e is DuplicateMethodSignatureException)
                .Select(e => (DuplicateMethodSignatureException)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethodsHandler) && e.MethodName=="DuplicateExposedStaticMethod"));

            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is MethodNotMarkedAsSlow)
                .Select(e => (MethodNotMarkedAsSlow)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethodsHandler) && e.MethodName=="NotSlowWithAddItem"));

            Assert.AreEqual(1, ex.InnerExceptions
                .Where(e => e is MethodWithAddItemNotVoid)
                .Select(e => (MethodWithAddItemNotVoid)e)
                .Count(e => e.ModelType==typeof(ModelWithInvalidExposedMethodsHandler) && e.MethodName=="SlowWithAddItemAndReturn"));
        }
    }
}
