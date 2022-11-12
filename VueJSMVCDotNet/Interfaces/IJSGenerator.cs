using System;
using System.Collections.Generic;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase);
    }
}
