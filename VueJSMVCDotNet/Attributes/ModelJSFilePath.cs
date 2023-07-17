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
        internal string Path { get; private init; }
        internal string MinPath
        {
            get { return (Path.EndsWith(".min.js") ? Path : string.Concat(Path.AsSpan(0, Path.LastIndexOf(".")), ".min.js")); }
        }

        /// <summary>
        /// Constructor for tagging the ModelJSPath
        /// </summary>
        /// <param name="path">The url path to identify what url to provide the javascript definition of this model to.</param>
        public ModelJSFilePath(string path)
        {
            Path = (!path.EndsWith(".js") ? path+".js" : path).ToLower();
        }

        internal bool IsMatch(string url)
        {
            return Path == url.ToLower() || MinPath == url.ToLower();
        }
    }
}
