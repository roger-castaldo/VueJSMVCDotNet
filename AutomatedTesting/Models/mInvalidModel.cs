using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mInvalidModel")]
    [ModelJSFilePath("/resources/scripts/mInvalidModel.js")]
    internal class mInvalidModel : IModel
    {
        public string id
        {
            get { return null; }
        }
    }
}
