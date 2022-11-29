using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization;
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
        private VueFileTokenizer tokenizer;
        private ITokenSection[] sections;
        private List<IParsedComponent> parsedElements;
        private readonly string name;
        private readonly string vuePath;

        public VueFileCompiler(TextReader tr,string name,string vuePath)
            : this(name, vuePath)
        {
            this.tokenizer = new VueFileTokenizer(tr);
        }

        public VueFileCompiler(string content, string name, string vuePath)
            : this(name,vuePath)
        {
            this.tokenizer = new VueFileTokenizer(content);
        }

        private VueFileCompiler(string name, string vuePath)
        {
            this.name=(name.EndsWith(".vue") ? name.Substring(0,name.Length-4) : name);
            this.vuePath=vuePath;
        }

        public string AsCompiled
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (this.parsedElements==null)
                {
                    int cacheCount = 0;
                    this.parsedElements = new List<IParsedComponent>();
                    this.sections = this.tokenizer.Tokenize();
                    for(int x = 0; x<this.sections.Length; x++)
                    {
                        if (this.sections[x] is IParsableComponent)
                        {
                            IParsedComponent[] tmp = ((IParsableComponent)this.sections[x]).Parse();
                            if (tmp!=null)
                                this.parsedElements.AddRange(tmp);
                        }
                    }
                    ClassPropertiesMap cpm = _ExtractPropertiesMap(this.parsedElements.ToArray());
                    this.parsedElements.Add(cpm);
                    foreach (ITokenSection its in this.sections)
                    {
                        if (!(its is IParsedComponent))
                        {
                            if (its is ScriptSection)
                                ((ScriptSection)its).Compile(ref sb, this.parsedElements.ToArray(), this.name, ref cacheCount,this.vuePath);
                            else 
                                its.Compile(ref sb, this.parsedElements.ToArray(), this.name, ref cacheCount);
                        }
                    }
                }
                sb.AppendLine(string.Format("export default __{0}__", this.name));
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
                        case "methods":
                            ret.ProcessMethodsValue(((ClassProperty)ipc).Content);
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
