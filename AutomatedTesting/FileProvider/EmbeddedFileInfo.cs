using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AutomatedTesting.FileProvider
{
    internal class EmbeddedFileInfo : IFileInfo
    {
        private static readonly Regex _regFileName = new Regex("^(.+)\\.([^.]+\\.[^.]{3,4})$", RegexOptions.Compiled|RegexOptions.ECMAScript);
        private static readonly Assembly _assembly = typeof(EmbeddedDirectoryContents).Assembly;
        private string _path;
        private bool _isFile;
        private EmbeddedResourceFileProvider _erfp;

        public EmbeddedFileInfo(string path)
            : this(path, null) { }

        public EmbeddedFileInfo(string path, EmbeddedResourceFileProvider erfp)
        {
            _erfp = erfp;
            _path=path;
            _isFile  = _assembly.GetManifestResourceNames()
                .Where(name => name.ToLower()==_path.ToLower())
                .Where(name=>(_erfp==null ? true : !_erfp.HiddenPaths.Contains(name)))
                .Count()==1;
        }

        public bool Exists =>_isFile||_assembly.GetManifestResourceNames()
                    .Where(name => name.ToLower().StartsWith(_path.ToLower()))
                    .Where(name => (_erfp==null ? true : !_erfp.HiddenPaths.Contains(name)))
                    .Count()>0;

        public long Length => (_isFile ? 
            (
                _assembly.GetManifestResourceStream(_path)==null ? 
                throw new FileNotFoundException() : 
                _assembly.GetManifestResourceStream(_path).Length
            ) 
            : 0);
       
        public string PhysicalPath
        {
            get { return _path.Replace('.', Path.DirectorySeparatorChar); }
        }

        public string Name => (_isFile ? _regFileName.Match(_path).Groups[2].Value : _path.Substring(_path.LastIndexOf('.')+1));

        public DateTimeOffset LastModified => new DateTimeOffset(DateTime.Today.AddDays(-1));

        public bool IsDirectory => !_isFile;

        public Stream CreateReadStream()
        {
            return _assembly.GetManifestResourceStream(_path);
        }
    }
}
