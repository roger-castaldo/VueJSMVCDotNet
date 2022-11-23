using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void ProcessPropsValue(string value)
        {
            foreach (string str in value.Split(','))
            {
                string tmp = value.Trim('[').Trim(']').Trim('\'').Trim('"');
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

        public string ProcessContent(string content)
        {
            if (_valueMaps.ContainsKey(content))
                return _valueMaps[content];
            return "_ctx."+content;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }
    }
}
