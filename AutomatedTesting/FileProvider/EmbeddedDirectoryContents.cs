using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutomatedTesting.FileProvider
{
    internal class EmbeddedDirectoryContents : IDirectoryContents
    {
        private static readonly Assembly _assembly = typeof(EmbeddedDirectoryContents).Assembly;
        private static readonly Regex _regFile = new Regex("^[^.]+\\.[^.]{3,4}$",RegexOptions.Compiled|RegexOptions.ECMAScript);

        private string _path;
        private string[] _children;

        public EmbeddedDirectoryContents(string path, EmbeddedResourceFileProvider erfp)
        {
            _path = path.Replace(Path.DirectorySeparatorChar,'.').ToLower();
            _children = _assembly.GetManifestResourceNames()
                    .Where(name => name.ToLower().StartsWith(_path))
                    .Where(name=>!erfp.HiddenPaths.Contains(name))
                    .ToArray();
            for(int i = 0; i < _children.Length; i++)
            {
                if (!_regFile.IsMatch(_children[i].Substring(_path.Length+1)))
                    _children[i] = _children[i].Substring(0, _children[i].IndexOf('.', _path.Length+1));
            }
            _children = _children.Distinct().ToArray();
        }

        public bool Exists
        {
            get
            {
                return _children.Length>0;
            }
        }

        private List<IFileInfo> _Files
        {
            get
            {
                List<IFileInfo> ret = new List<IFileInfo>();
                foreach (string path in _children)
                    ret.Add(new EmbeddedFileInfo(path));
                return ret;
            }
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return _Files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Files.GetEnumerator();
        }
    }
}
