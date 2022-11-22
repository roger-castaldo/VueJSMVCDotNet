using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class VariableChunk : IToken, ICompileable
    {
        private string _value;

        public VariableChunk(string value)
        {
            _value = value.TrimStart('{').TrimEnd('}');
        }

        public string AsString => string.Format("{{{{{0}}}}}", _value);

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            sb.Append(_value);
        }
    }
}
