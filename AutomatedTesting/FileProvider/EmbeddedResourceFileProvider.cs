using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutomatedTesting.FileProvider
{
    internal class EmbeddedResourceFileProvider : IFileProvider
    {
        public const string basePath = "AutomatedTesting";

        private EmbeddedChangeToken _changeToken;
        private List<string> _hiddenPaths;
        public List<string> HiddenPaths { get { return _hiddenPaths; } }

        public void HidePath(string path)
        {
            _hiddenPaths.Add(path);
            _changeToken.Trigger();
        }

        public void ShowPath(string path)
        {
            _hiddenPaths.Remove(path);
            _changeToken.Trigger();
        }

        public EmbeddedResourceFileProvider()
        {
            _changeToken=new EmbeddedChangeToken();
            _hiddenPaths=new List<string>();
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return new EmbeddedDirectoryContents(string.Format("{0}.{1}",new object[] { basePath, subpath }),this);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return new EmbeddedFileInfo(String.Format("{0}.{1}", new object[] { basePath, subpath.Replace(Path.DirectorySeparatorChar, '.') }),this);
        }

        public IChangeToken Watch(string filter)
        {
            return _changeToken;
        }
    }
}
