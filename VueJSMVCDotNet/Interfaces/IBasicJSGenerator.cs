using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IBasicJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder);
    }
}
