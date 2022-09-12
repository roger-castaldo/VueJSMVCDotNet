using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the js path that the javascript code for this model will be written to
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelJSFilePath: Attribute
    {
        private string _path;
        internal string Path
        {
            get { return _path; }
        }

        private string _modelNamespace;
        internal string ModelNamespace { get { return _modelNamespace; } }

        internal string MinPath
        {
            get { return (_path.EndsWith(".min.js") ? _path : _path.Substring(0, _path.LastIndexOf(".")) + ".min.js"); }
        }

        /// <summary>
        /// Constructor for tagging the ModelJSPath
        /// </summary>
        /// <param name="path">The url path to identify what url to provide the javascript definition of this model to.</param>
        /// <param name="modelNamespace">Optional:  The namespace to build this model into on the javascript side.  Default is the default passed to the handler.</param>
        public ModelJSFilePath(string path, string modelNamespace = null)
        {
            _path = (!path.EndsWith(".js") ? path+".js" : path).ToLower();
            _modelNamespace= modelNamespace;
        }

        internal bool IsMatch(string url)
        {
            return Path == url.ToLower() || MinPath == url.ToLower();
        }
    }
}
