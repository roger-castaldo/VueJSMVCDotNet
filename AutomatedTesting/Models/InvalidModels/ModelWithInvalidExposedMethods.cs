using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithInvalidExposedMethods.js")]
    [ModelRoute("/models/ModelWithInvalidExposedMethods")]
    internal class ModelWithInvalidExposedMethods : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithInvalidExposedMethods Load(string id)
        {
            return null;
        }

        [ExposedMethod()]
        public static void DuplicateExposedStaticMethod(int par1) { }

        [ExposedMethod()]
        public static void DuplicateExposedStaticMethod(string par1) { }

        [ExposedMethod]
        public static void NotSlowWithAddItem(AddItem addItem) { }

        [ExposedMethod(isSlow:true)]
        public static bool SlowWithAddItemAndReturn(AddItem addItem) { return true; }
    }
}
