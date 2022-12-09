using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives
{
    internal class BindDirective : IVueDirective, ICompileable, IParsableComponent
    {
        private string _command;
        public string Command { get { return _command; } }
        private string _value;
        public string Value { get { return _value; } }

        public BindDirective(string command, string value)
        {
            _command=command;
            _value=value;
        }

        public string AsString => string.Format("v-bind:{0}=\"{1}\"", new object[] { _command, _value });

        public int Cost
        {
            get {
                int cost = 1;
                switch (_command)
                {
                    case "class":
                        cost=cost<<1;
                        break;
                    case "style":
                        cost=cost<<2;
                        break;
                    default:
                        cost=cost<<3;
                        break;
                }
                return cost;
            }
        }

        public void Compile(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount)
        {
            switch (_command.ToLower())
            {
                case "class":
                    sb.AppendFormat("class:_normalizeClass({0})", new object[] { VueFileCompiler.ProcessClassProperties(components, _value) });
                    break;
                case "style":
                    sb.AppendFormat("style:_normalizeStyle({0})", new object[] { VueFileCompiler.ProcessClassProperties(components, _value) });
                    break;
                default:
                    sb.AppendFormat("{0}:{1}", new object[] { _command, VueFileCompiler.ProcessClassProperties(components, _value) });
                    break;
            }
        }

        public IParsedComponent[] Parse()
        {
            switch (_command.ToLower())
            {
                case "class":
                    return new IParsedComponent[] { new Import(new string[] { "normalizeClass as _normalizeClass" }, Constants.VUE_IMPORT_NAME) };
                    break;
                case "style":
                    return new IParsedComponent[] { new Import(new string[] { "normalizeStyle as _normalizeStyle" }, Constants.VUE_IMPORT_NAME) };
                    break;
                default:
                    return null;
                    break;
            }
        }
    }
}
