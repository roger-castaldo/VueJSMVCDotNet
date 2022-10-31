using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    internal interface IJSClassGenerator
    {
        void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, PropertyInfo[] properties, string urlBase);
    }
}
