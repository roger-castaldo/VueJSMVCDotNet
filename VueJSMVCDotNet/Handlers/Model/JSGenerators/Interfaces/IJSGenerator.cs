using System;
using System.Collections.Generic;
using System.Text;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces
{
    internal interface IJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase,ILog log);
    }
}
