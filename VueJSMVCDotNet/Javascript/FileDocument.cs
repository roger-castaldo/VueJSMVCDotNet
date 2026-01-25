using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using System.IO;

namespace VueJSMVCDotNet.Javascript
{
    internal class FileDocument(Stream content, string name) : Document
    {
        public override DocumentInfo Info => new(name)
        {
            Category = ModuleCategory.Standard
        };

        public override Stream Contents => content;
    }
}
