using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives
{
    internal class KeyDirective : IVueDirective, ICompileable
    {
        private string _value;
        public KeyDirective(string value) { _value = value; }
        public string AsString => string.Format("v-bind:key=\"{0}\"", _value);

        public int Cost => 0;

        public void Compile(ref StringBuilder sb, IParsedComponent[] components, string name)
        {
            sb.AppendFormat("key:{0}", new object[] { VueFileCompiler.ProcessClassProperties(components, _value) });
        }
    }
}
