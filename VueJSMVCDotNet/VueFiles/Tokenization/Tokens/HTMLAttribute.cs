using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class HTMLAttribute : IToken,ICompileable
    {
        private string _name;
        public string Name { get { return _name; } }
        private string _value=null;
        public string Value { get { return _value; } }

        public HTMLAttribute(string content)
        {
            if (content.Contains("="))
            {
                _name = content.Substring(0, content.IndexOf('='));
                _value=content.Substring(content.IndexOf('=')+1).Trim('\"');
            }
            else
                _name= content;
        }

        public string AsString
        {
            get
            {
                return (_value==null ? _name : string.Format("{0}=\"{1}\"", new object[] { _name, _value }));
            }
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount)
        { 
            sb.AppendFormat("{0}:\"{1}\"", new object[] { _name, (_value==null ? true.ToString().ToLower() : _value) });
        }
    }
}
