using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives
{
    internal class ModelDirective : IWithVueDirective, IEventDirective
    {
        private string _value;
        public string Value { get { return _value; } }
        public ModelDirective(string value)
        {
            _value=value;
        }

        public string AsString => String.Format("v-model=\"{0}\"",_value);

        public int Cost => 0;

        public IParsedComponent[] Parse()
        {
            return new IParsedComponent[]
            {
                new Import(new string[]
                {
                    "vModelCheckbox as _vModelCheckbox",
                    "vModelDynamic as _vModelDynamic",
                    "vModelRadio as _vModelRadio",
                    "vModelSelect as _vModelSelect",
                    "vModelText as _vModelText"
                },
                Constants.VUE_IMPORT_NAME)
            };
        }

        public void ProduceDirective(ref StringBuilder sb, IParsedComponent[] components, string name, IHTMLElement owner)
        {
            sb.Append("[");
            if (owner.InputType!=null)
            {
                switch (owner.InputType)
                {
                    case "text":
                    case "color":
                    case "date":
                    case "datetime-local":
                    case "email":
                    case "hidden":
                    case "month":
                    case "number":
                    case "password":
                    case "search":
                    case "tel":
                    case "time":
                    case "url":
                    case "week":
                        sb.Append("_vModelText");
                        break;
                    case "checkbox":
                        sb.Append("_vModelCheckbox");
                        break;
                    case "radio":
                        sb.Append("_vModelRadio");
                        break;
                    default:
                        if (owner.Tag=="select")
                            sb.Append("_vModelSelect");
                        else
                            sb.Append("_vModelDynamic");
                        break;
                }
            }
            sb.AppendFormat(",{0}]", VueFileCompiler.ProcessClassProperties(components, _value));
        }

        public void ProduceEvent(ref StringBuilder sb, IParsedComponent[] components, string name, ref int cacheCount, IHTMLElement owner, bool isSetup)
        {
            sb.AppendFormat("\"onUpdate:modelValue\": _cache[{0}] || (_cache[{0}] = $event => ({1} = $event))", new object[]
            {
                cacheCount,
                VueFileCompiler.ProcessClassProperties(components,_value)
            });
            cacheCount++;
        }
    }
}
