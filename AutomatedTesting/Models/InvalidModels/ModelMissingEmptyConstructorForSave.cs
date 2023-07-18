using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelMissingEmptyConstructorForSave.js")]
    [ModelRoute("/models/ModelMissingEmptyConstructorForSave")]
    internal class ModelMissingEmptyConstructorForSave : IModel
    {

        public string id => null;

        [ModelLoadMethod()]
        public static ModelMissingEmptyConstructorForSave Load(string id)
        {
            return null;
        }

        public ModelMissingEmptyConstructorForSave(bool noEmpty) { }

        [ModelSaveMethod()]
        public bool Save () { return true; }
    }
}
