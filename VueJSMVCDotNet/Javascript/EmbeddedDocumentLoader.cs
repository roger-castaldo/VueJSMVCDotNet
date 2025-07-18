using Microsoft.ClearScript;
using System.IO;

namespace VueJSMVCDotNet.Javascript
{
    internal class EmbeddedDocumentLoader(Dictionary<string, string> documents) : Microsoft.ClearScript.DocumentLoader
    {
        public override Task<Document> LoadDocumentAsync(DocumentSettings settings, DocumentInfo? sourceInfo, string specifier, DocumentCategory category, DocumentContextCallback contextCallback)
        {
            if (documents.TryGetValue(specifier, out var path))
            {
                var result = new FileDocument(typeof(JSEngine).Assembly.GetManifestResourceStream(path)!, specifier);
                return Task.FromResult<Document>(result);
            }
            throw new FileNotFoundException(path);
        }
    }
}
