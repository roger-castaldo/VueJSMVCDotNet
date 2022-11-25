using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents
{
    internal class Import : IParsedComponent, ICompileable,IScriptHeader
    {

        private string[] _importedElements;
        public string[] ImportElements { get { return _importedElements; } }
        private string _importPath;
        public string ImportPath { get { return _importPath; } }

        public Import(string[] elements, string path)
        {
            _importedElements=elements;
            _importPath=path;
            if (_importPath.StartsWith("'"))
                _importPath=_importPath.Trim('\'');
            else if (_importPath.StartsWith("\""))
                _importPath=_importPath.Trim('"');
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            sb.Append("import ");
            if (_importedElements.Length>1)
            {
                sb.Append("{");
                foreach (string str in _importedElements)
                    sb.AppendFormat("{0},", str);
                sb.Length=sb.Length-1;
                sb.Append("}");
            }
            else if (_importedElements.Length==1)
                sb.Append(_importedElements[0]);
            sb.AppendLine(string.Format("{0}'{1}';", new object[] { (_importedElements.Length!=0 ? " from " : ""), _importPath }));
        }
    }
}
