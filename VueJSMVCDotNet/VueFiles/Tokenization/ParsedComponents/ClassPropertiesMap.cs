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
        private Dictionary<string, string> _valueMaps;

        public ClassPropertiesMap()
        {
            _valueMaps= new Dictionary<string, string>();
        }

        private static readonly Regex _regInvalidChars = new Regex("[^A-Za-z0-9$_]", RegexOptions.Compiled|RegexOptions.ECMAScript);
        public void ProcessPropsValue(string value)
        {
            foreach (string str in value.Split(','))
            {
                string tmp = _regInvalidChars.Replace(str, "");
                _valueMaps.Add(tmp, string.Format("$props.{0}", tmp));
            }
        }

        internal void ProcessComputedValue(string content)
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
                            _valueMaps.Add(prop.Trim(), string.Format("$options.{0}", prop.Trim()));
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
                _valueMaps.Add(prop.Trim(), string.Format("$options.{0}", prop.Trim()));
        }

        internal void ProcessDataValue(string content)
        {
            content = content.Substring(content.IndexOf("return")+"return".Length).Trim().TrimEnd('}');
            content = content.Trim().TrimStart('{').TrimEnd("};".ToArray());
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
                            _valueMaps.Add(prop.Trim(), string.Format("_ctx.{0}", prop.Trim()));
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
                _valueMaps.Add(prop.Trim(), string.Format("_ctx.{0}", prop.Trim()));
        }

        internal void ProcessMethodsValue(string content)
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
                            _valueMaps.Add(prop.Trim(), string.Format("_ctx.{0}()", prop.Trim()));
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
                _valueMaps.Add(prop.Trim(), string.Format("_ctx.{0}()", prop.Trim()));
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
                    ||(variable!="" && _VALID_FIRST_VARIABLE_CHARACTERS.Contains(c)))
                    variable+=c;
                else
                {
                    if (variable!="")
                    {
                        if (_valueMaps.ContainsKey(variable))
                            ret+=_valueMaps[variable];
                        else
                            ret+="_ctx."+variable;
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
                    ret+="_ctx."+variable;
            }
            return ret;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }
    }
}
