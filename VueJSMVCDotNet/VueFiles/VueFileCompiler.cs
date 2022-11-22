using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles
{
    internal class VueFileCompiler
    {
        private VueFileTokenizer _tokenizer;
        private ITokenSection[] _sections;
        private List<IParsedComponent> _parsedElements;
        private string _name;

        public VueFileCompiler(TextReader tr,string name)
        {
            _tokenizer = new VueFileTokenizer(tr);
            _name=name;
        }

        public VueFileCompiler(string content, string name)
        {
            _tokenizer = new VueFileTokenizer(content);
            _name=name;
        }

        public string AsCompiled
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (_parsedElements==null)
                {
                    _parsedElements = new List<IParsedComponent>();
                    _sections = _tokenizer.Tokenize();
                    for(int x = 0; x<_sections.Length; x++)
                    {
                        if (_sections[x] is IParsableComponent)
                        {
                            IParsedComponent[] tmp = ((IParsableComponent)_sections[x]).Parse();
                            if (tmp!=null)
                                _parsedElements.AddRange(tmp);
                        }
                    }
                    ClassPropertiesMap cpm = _ExtractPropertiesMap(_parsedElements.ToArray());
                    _parsedElements.Add(cpm);
                    foreach (ITokenSection its in _sections)
                    {
                        if (!(its is IParsedComponent))
                            its.Compile(ref sb, _parsedElements.ToArray(), _name);
                    }
                }
                sb.AppendLine(string.Format("export default __{0}__", _name));
                return sb.ToString();
            }
        }

        private static ClassPropertiesMap _ExtractPropertiesMap(IParsedComponent[] components)
        {
            ClassPropertiesMap ret = new ClassPropertiesMap();
            foreach (IParsedComponent ipc in components)
            {
                if (ipc is ClassProperty)
                {
                    switch (((ClassProperty)ipc).Name)
                    {
                        case "props":
                            ret.ProcessPropsValue(((ClassProperty)ipc).Content);
                            break;
                        case "computed":
                            ret.ProcessComputedValue(((ClassProperty)ipc).Content);
                            break;
                        case "data":
                            ret.ProcessDataValue(((ClassProperty)ipc).Content);
                            break;
                    }
                }
            }
            return ret;
        }

        public static string ProcessClassProperties(IParsedComponent[] components,string content)
        {
            string ret = content;
            foreach (IParsedComponent ipc in components)
            {
                if (ipc is ClassPropertiesMap)
                {
                    ret = ((ClassPropertiesMap)ipc).ProcessContent(content);
                    break;
                }
            }
            return ret;
        }
    }
}
