using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal class VueDirective : IToken,ICompileable, IParsableComponent,IPatchable
    {
        private string _command;
        public string Command { get { return _command; } }

        private string _value;
        public string Value { get { return _value; } }
        private int _cost = 1;

        public VueDirective(string command, string value)
        {
            _command=command;
            if (_command.StartsWith(":"))
                _command="v-bind"+_command;
            _value=value;
            switch (_command)
            {
                case "v-bind:class":
                    _cost=_cost<<1;
                    break;
                case "v-bind:style":
                    _cost=_cost<<2;
                    break;
            }
        }

        public string AsString => string.Format("{0}=\"{1}\"", new object[] { _command,_value });

        public int Cost => _cost;

        public void Compile(ref StringBuilder sb, IParsedComponent[] components,string name)
        {
            if (_command.StartsWith("v-on:"))
            {
                string evnt = _command.Split(':')[1];
                evnt = evnt[0].ToString().ToUpper()+evnt.Substring(1);
                sb.AppendFormat("on{0}", evnt);
                sb.AppendFormat("(event){{{0}}}", new object[] { _value.TrimStart('"').TrimEnd('"') });
            }
            else if (_command.Contains(":"))
            {
                switch (_command.ToLower())
                {
                    case "v-bind:class":
                        sb.AppendFormat("class:_normalizeClass({0})", new object[] { VueFileCompiler.ProcessClassProperties(components,_value) });
                        break;
                    case "v-bind:style":
                        sb.AppendFormat("style:_normalizeStyle({0})", new object[] { VueFileCompiler.ProcessClassProperties(components, _value) });
                        break;
                    default:
                        sb.AppendFormat("{0}:{1}", new object[] { _command.Split(':')[1], VueFileCompiler.ProcessClassProperties(components, _value) });
                        break;
                }
            }
            else
                sb.Append(_command);
        }

        public IParsedComponent[] Parse()
        {
            switch (_command.ToLower())
            {
                case "v-bind:class":
                case ":class":
                case "class":
                    return new IParsedComponent[] { new Import(new string[] { "normalizeClass as _normalizeClass" }, "vue") };
                    break;
                case "v-bind:style":
                case ":style":
                case "style":
                    return new IParsedComponent[] { new Import(new string[] { "normalizeStyle as _normalizeStyle" }, "vue") };
                    break;
                default:
                    return null;
                    break;
            }
        }
    }
}
