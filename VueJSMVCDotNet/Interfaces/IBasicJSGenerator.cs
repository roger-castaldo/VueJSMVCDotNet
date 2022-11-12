using System;
using System.Collections.Generic;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IBasicJSGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder,string urlBase, sModelType[] models);
    }
}
