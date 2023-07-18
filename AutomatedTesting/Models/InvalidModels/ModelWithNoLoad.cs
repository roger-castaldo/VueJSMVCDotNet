using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithNoLoad.js")]
    [ModelRoute("/models/ModelWithNoLoad")]
    internal class ModelWithNoLoad : IModel
    {
        public string id => null;

    }
}
