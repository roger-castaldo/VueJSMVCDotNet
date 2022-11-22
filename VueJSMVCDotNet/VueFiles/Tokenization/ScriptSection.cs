using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization
{
    internal class ScriptSection : ITokenSection,IParsableComponent
    {
        private bool _isSetup;
        public bool IsSetup { get { return _isSetup; } }

        private IToken[] _content;

        public string AsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<script>");
                foreach (IToken child in _content)
                    sb.AppendLine(child.AsString);
                sb.AppendLine("</script>");
                return sb.ToString();
            }
        }

        public ScriptSection(bool isSetup,IToken[] content)
        {
            _isSetup = isSetup;
            _content=content;
        }

        private IToken[] _strippedComponents;

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            components = _MergeImports(components);
            foreach (IParsedComponent component in components)
            {
                if (component is ICompileable && component is IScriptHeader)
                    ((ICompileable)component).Compile(ref sb, components,name);
            }
            foreach (IToken child in _strippedComponents)
            {
                if (child is ICompileable)
                    ((ICompileable)child).Compile(ref sb, components, name);
                else
                    sb.AppendLine(child.AsString);
            }
            foreach (IParsedComponent component in components)
            {
                if (component is ICompileable && !(component is IScriptHeader) && !(component is IComponentProperty))
                    ((ICompileable)component).Compile(ref sb, components,name);
            }
            sb.AppendLine(string.Format("const __{0}__ = {{", name));
            foreach (IParsedComponent component in components)
            {
                if (component is IComponentProperty && component is ICompileable)
                {
                    sb.Append("\t");
                    ((ICompileable)component).Compile(ref sb, components, name);
                    sb.AppendLine(",");
                }
            }
            sb.AppendLine(string.Format(@"__file: '{0}.vue'
}};",name));
        }

        private IParsedComponent[] _MergeImports(IParsedComponent[] components)
        {
            List<IParsedComponent> ret = new List<IParsedComponent>(components);
            for(int x = 0; x<ret.Count; x++)
            {
                if (ret[x] is Import)
                {
                    List<string> tmp = new List<string>();
                    tmp.AddRange(((Import)ret[x]).ImportElements);
                    for(int y = x+1; y<ret.Count; y++)
                    {
                        if (ret[y] is Import)
                        {
                            if (((Import)ret[y]).ImportPath == ((Import)ret[x]).ImportPath)
                            {
                                tmp.AddRange(((Import)ret[y]).ImportElements);
                                ret.RemoveAt(y);
                                y--;
                            }
                        }
                    }
                    string path = ((Import)ret[x]).ImportPath;
                    ret.RemoveAt(x);
                    ret.Insert(x, new Import(tmp.GroupBy(g=>g.ToString()).Select(g=>g.First()).ToArray(), path));
                }
            }
            return ret.ToArray();
        }

        private static readonly Regex _RegImportStatement = new Regex("^\\s*import\\s+(\\{[^\\}]+\\}|[^\\s]+)\\s+from\\s+(\"[^\"]+\"|'[^']+')\\s*;\\s*$", RegexOptions.Compiled|RegexOptions.Multiline);

        public IParsedComponent[] Parse()
        {
            List<IParsedComponent> ret = new List<IParsedComponent>();
            List<IToken> stripped = new List<IToken>();
            foreach (IToken child in _content)
            {
                string content = child.AsString;
                Match m = _RegImportStatement.Match(content);
                while (m.Success)
                {
                    ret.Add(new Import(m.Groups[1].Value.Split(','), m.Groups[2].Value));
                    content = content.Replace(m.Value, "");
                    m = _RegImportStatement.Match(content);
                }

                if (_isSetup)
                {

                }
                else if (content.Contains("export default"))
                {
                    stripped.Add(new TextToken(content.Substring(0, content.IndexOf("export default"))));
                    content = content.Substring(content.IndexOf("export default")+"export default".Length);
                    content = content.Substring(content.IndexOf("{"));
                    content = _ProcessDefaultExport(content, ref ret);
                }
                if (content.Trim()!="")
                    stripped.Add(new TextToken(content));
            }
            _strippedComponents=stripped.ToArray();
            return ret.ToArray();
        }

        private string _ProcessDefaultExport(string content, ref List<IParsedComponent> components)
        {
            content = content.Substring(content.IndexOf("{")+1);
            string prop = "";
            string value = "";
            for(int idx = 0; idx<content.Length; idx++)
            {
                switch (content[idx])
                {
                    case ':':
                        prop = prop.Trim();
                        value="";
                        break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case ',':
                        break;
                    case '[':
                        value+=content[idx];
                        idx++;
                        while(idx<content.Length && content[idx]!=']')
                        {
                            value+=content[idx];
                            idx++;
                        }
                        components.Add(new ClassProperty(prop, value+"]"));
                        prop="";
                        value="";
                        break;
                    case '{':
                        if (prop.EndsWith("function()"))
                        {
                            value=prop.Substring(prop.Length-"function()".Length);
                            prop=prop.Substring(0, prop.IndexOf("function()"));
                        }
                        value+=content[idx];
                        idx++;
                        int bracketCount = 1;
                        while (idx<content.Length && bracketCount>0)
                        {
                            value+=content[idx];
                            if (content[idx]=='{')
                                bracketCount++;
                            else if (content[idx]=='}')
                                bracketCount--;
                            idx++;
                        }
                        components.Add(new ClassProperty(prop, value));
                        prop="";
                        value="";
                        break;
                    case '}':
                        content = content.Substring(idx+1);
                        idx=content.Length;
                        break;
                    default:
                        prop+=content[idx];
                        break;
                }
            }
            return content;
        }

        public int CompareTo(object obj)
        {
            if (obj is ScriptSection)
            {
                if (((ScriptSection)obj)._isSetup)
                    return -1;
                else if (_isSetup)
                    return 1;
                else
                    return 0;
            }
            return -1;
        }
    }
}
