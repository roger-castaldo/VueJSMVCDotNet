using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithBlockedID.js")]
    [ModelRoute("/models/ModelWithBlockedID")]
    internal class ModelWithBlockedID : IModel
    {
        [ModelIgnoreProperty]
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithBlockedID Load(string id)
        {
            return null;
        }

    }
}
