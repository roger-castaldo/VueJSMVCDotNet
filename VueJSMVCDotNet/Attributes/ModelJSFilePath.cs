using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Attributes
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

        internal string MinPath
        {
            get { return (_path.EndsWith(".min.js") ? _path : _path.Substring(0, _path.LastIndexOf(".")) + ".min.js"); }
        }

        /// <summary>
        /// Constructor for tagging the ModelJSPath
        /// </summary>
        /// <param name="path">The url path to identify what url to provide the javascript definition of this model to.</param>
        public ModelJSFilePath(string path)
        {
            _path = (!path.EndsWith(".js") ? path+".js" : path).ToLower();
        }

        internal bool IsMatch(string url)
        {
            return Path == url.ToLower() || MinPath == url.ToLower();
        }
    }
}
