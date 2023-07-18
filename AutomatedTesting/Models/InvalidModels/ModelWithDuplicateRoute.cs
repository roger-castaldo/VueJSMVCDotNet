using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithDuplicateMethods.js")]
    [ModelRoute("/models/ModelWithDuplicateMethods")]
    internal class ModelWithDuplicateRoute : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithDuplicateRoute Load(string id)
        {
            return null;
        }
    }
}
