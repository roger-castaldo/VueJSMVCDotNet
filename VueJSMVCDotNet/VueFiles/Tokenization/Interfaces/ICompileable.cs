using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces
{
    internal interface ICompileable
    {
        void Compile(ref StringBuilder sb, IParsedComponent[] components,string name,ref int cacheCount);
    }
}
