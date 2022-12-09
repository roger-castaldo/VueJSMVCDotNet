using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents
{
    internal class DeclaredConstant : IParsedComponent,ICompileable
    {
        private string _name;
        public string Name { get { return _name; } }
        private string _value;
        public string Value { get { return _value; } }
        public DeclaredConstant(string name, string value)
        {
            _name=name;
            _value=value;
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount)
        {
            sb.AppendLine(string.Format("const {0} = {1};", new object[]
            {
                _name,
                _value
            }));
        }

        public override bool Equals(object obj)
        {
            if (obj is DeclaredConstant)
            {
                DeclaredConstant dc = (DeclaredConstant)obj;
                return dc.Name == _name && dc.Value == _value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
