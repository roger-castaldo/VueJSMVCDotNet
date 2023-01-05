using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithDuplicateMethods.js")]
    [ModelRoute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateMethods : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithDuplicateMethods Load(string id)
        {
            return null;
        }

        [ModelLoadMethod()]
        public static ModelWithDuplicateMethods DuplicateLoadMethod(string id)
        {
            return null;
        }

        [ModelLoadAllMethod()]
        public static List<ModelWithDuplicateMethods> LoadAll() { return null; }

        [ModelLoadAllMethod()]
        public static List<ModelWithDuplicateMethods> DuplicateLoadAllMethod() { return null; }

        public ModelWithDuplicateMethods() { }


        [ModelSaveMethod()]
        public bool Save() { return true; }

        [ModelSaveMethod]
        public bool DuplicateSaveMethod() { return true; }

        [ModelDeleteMethod()]
        public bool Delete() { return true; }

        [ModelDeleteMethod]
        public bool DuplicateDeleteMethod() { return true; }

        [ModelUpdateMethod()]
        public bool Update() { return true; }

        [ModelUpdateMethod]
        public bool DuplicateUpdateMethod() { return true; }
    }
}
