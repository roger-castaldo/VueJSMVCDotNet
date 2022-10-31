using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase);
    }
}
