using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents
{
    internal class ClassProperty : IComponentProperty,IComparable, ICompileable
    {
        private string _name;
        public string Name { get { return _name; } }
        private string _content;
        public string Content { get { return _content; } }
        public ClassProperty(string name, string content)
        {
            _name=name;
            _content=content;
        }

        private static readonly List<string> _SORT_ORDER = new List<string>(new string[] { "props", "emits", "data", "computed", "watch", "methods", "expose" });

        public int CompareTo(object obj)
        {
            int ret = -1;
            if (obj is ClassProperty)
            {
                if (_SORT_ORDER.Contains(_name))
                {
                    if (_SORT_ORDER.Contains(((ClassProperty)obj)._name))
                        return _SORT_ORDER.IndexOf(_name)-_SORT_ORDER.IndexOf(((ClassProperty)obj)._name);
                }
                else if (_SORT_ORDER.Contains(((ClassProperty)obj)._name))
                    ret=1;
            }
            return ret;
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name, ref int cacheCount)
        {
            sb.AppendFormat("{0}:{1}", new object[] { _name, _content });
        }
    }
}
