using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models.InvalidModels
{
    [ModelJSFilePath("/resources/scripts/ModelWithNoRoute.js")]
    internal class ModelWithNoRoute : IModel
    {
        public string id => null;

        [ModelLoadMethod()]
        public static ModelWithNoRoute Load(string id)
        {
            return null;
        }
    }
}
