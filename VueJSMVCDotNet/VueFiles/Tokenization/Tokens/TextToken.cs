using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class TextToken : IToken,IPatchable
    {
        private string _value;
        public TextToken(string value)
        {
            _value=value;
        }

        public string AsString => _value;

        public int Cost => 1;
    }
}
