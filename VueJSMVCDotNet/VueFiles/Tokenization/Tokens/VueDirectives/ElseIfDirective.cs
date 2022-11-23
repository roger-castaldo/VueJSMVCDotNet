using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives
{
    internal class ElseIfDirective : IVueDirective
    {
        private string _value;
        public string Value { get { return _value; } }
        public ElseIfDirective(string value)
        {
            _value=value;
        }

        public string AsString => string.Format("v-else-if=\"{0}\"",_value);

        public int Cost => 0;
    }
}
