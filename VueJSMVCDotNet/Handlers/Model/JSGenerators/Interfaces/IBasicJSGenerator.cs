using System;
using System.Collections.Generic;
using System.Text;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces
{
    internal interface IBasicJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder,string urlBase, sModelType[] models, ILog log);
    }
}
