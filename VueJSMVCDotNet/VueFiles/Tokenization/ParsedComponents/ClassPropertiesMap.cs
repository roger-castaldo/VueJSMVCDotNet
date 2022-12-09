using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents
{
    internal class ClassPropertiesMap : IComponentProperty
    {
        private bool _useSetup;
        private Dictionary<string, string> _valueMaps;

        public ClassPropertiesMap()
        {
            _valueMaps= new Dictionary<string, string>()
            {
                {"null","null" },
                {"undefined","undefined" }
            };
        }

        private static readonly Regex _regInvalidChars = new Regex("[^A-Za-z0-9$_]", RegexOptions.Compiled|RegexOptions.ECMAScript);

        private void ProcessJsonProps(string content, string attribute, string ending = null)
        {
            content=content.Trim().TrimStart('{').TrimEnd('}');
            string prop = "";
            int bracketCount = 0;
            foreach (char c in content)
            {
                switch (c)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                    case '\n':
                    case ',':
                        break;
                    case '{':
                        prop="";
                        bracketCount++;
                        break;
                    case '}':
                        bracketCount--;
                        break;
                    case ':':
                        if (prop!="")
                        {
                            _valueMaps.Add(prop.Trim(), string.Format("{0}.{1}{2}", new object[] { attribute, prop.Trim(), ending }));
                            prop="";
                        }
                        break;
                    default:
                        if (bracketCount==0)
                            prop+=c;
                        break;
                }
            }
            if (prop.Trim()!="")
                _valueMaps.Add(prop.Trim(), string.Format("{0}.{1}{2}", new object[] { attribute, prop.Trim(), ending }));
        }

        public void ProcessPropsValue(string value)
        {
            if (value.StartsWith("["))
            {
                foreach (string str in value.Split(','))
                {
                    string tmp = _regInvalidChars.Replace(str, "");
                    _valueMaps.Add(tmp, string.Format("$props.{0}", tmp));
                }
            }
            else
                ProcessJsonProps(value, "$props");
                
        }

        internal void ProcessComputedValue(string content)
        {
            ProcessJsonProps(content, "$options");
        }

        internal void ProcessDataValue(string content)
        {
            content = content.Substring(content.IndexOf("return")+"return".Length).Trim().TrimEnd('}');
            content = content.Trim().TrimStart('{').TrimEnd("};".ToArray());
            ProcessJsonProps(content, "_ctx");
        }

        internal void ProcessMethodsValue(string content)
        {
            ProcessJsonProps(content, "$options");
        }

        private static readonly Regex _regRefs = new Regex("const\\s+([A-Za-z0-9$_]+)\\s*=\\s*(ref|reactive)\\(", RegexOptions.Compiled|RegexOptions.ECMAScript);
        private static readonly Regex _regDefineProps = new Regex("const\\s+([A-Za-z0-9$_]+)\\s*=\\s*defineProps\\(", RegexOptions.Compiled|RegexOptions.ECMAScript);
        internal string ProcessSetupScript(string content,ref List<IParsedComponent> components)
        {
            _useSetup=true;
            foreach (Match m in _regRefs.Matches(content))
                _valueMaps.Add(m.Groups[1].Value, String.Format("{0}.value", m.Groups[1].Value));
            if (_regDefineProps.IsMatch(content))
            {
                Match m = _regDefineProps.Match(content);
                string props = "";
                int bracketCount = 1;
                for(int x = m.Index+m.Length; x<content.Length; x++)
                {
                    if (content[x]==')')
                        bracketCount--;
                    else
                        props+=content[x];
                    if (bracketCount==0)
                    {
                        if (content[x+1]==';')
                            x++;
                        content = content.Substring(0, m.Index)+content.Substring(x+1);
                        ProcessJsonProps(props, m.Groups[1].Value);
                        components.Add(new ClassProperty("props", props));
                        components.Add(new DeclaredConstant(m.Groups[1].Value, "__props"));
                        break;
                    }
                }
            }
            return content;
        }

        const string _VALID_FIRST_VARIABLE_CHARACTERS = "abcedfghijklmnopqrstuvwxyz$_ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string _VALID_VARIABLE_CHARACTERS = _VALID_FIRST_VARIABLE_CHARACTERS+"0123456789";
        public string ProcessContent(string content)
        {
            string ret = "";
            string variable = "";
            for(int x=0;x<content.Length;x++)
            {
                char c = content[x];
                if ((variable=="" && _VALID_FIRST_VARIABLE_CHARACTERS.Contains(c))
                    ||(variable!="" && _VALID_VARIABLE_CHARACTERS.Contains(c)))
                    variable+=c;
                else
                {
                    if (variable!="")
                    {
                        if (_valueMaps.ContainsKey(variable))
                            ret+=_valueMaps[variable];
                        else
                            ret+=(_useSetup ? "" : "_ctx.")+variable;
                        variable="";
                    }
                    switch (c)
                    {
                        case '\'':
                        case '"':
                            ret+=c;
                            x++;
                            while(x<content.Length && content[x]!=c)
                            {
                                ret+=content[x];
                                x++;
                            }
                            ret+=content[x];
                            break;
                        default:
                            ret+=c;
                            break;
                    }
                }
            }
            if (variable!="")
            {
                if (_valueMaps.ContainsKey(variable))
                    ret+=_valueMaps[variable];
                else
                    ret+=(_useSetup ? "" : "_ctx.")+variable;
            }
            return ret;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }
    }
}
