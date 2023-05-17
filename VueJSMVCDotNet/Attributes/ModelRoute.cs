using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the route(path) to use for accessing the model 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelRoute : Attribute
    {
        private string _host;
        internal string Host
        {
            get { return _host; }
        }

        private string _path;
        internal string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Define the base route for the model that all rest paths will be built off of.
        /// </summary>
        /// <param name="path">The base path for the model's rest calls</param>
        /// <param name="host">(Optional) specify a host that is used, in the case of using more than one host.</param>
        public ModelRoute(string path,string host="*")
        {
            _path = path;
            _host = "*";
        }
    }
}
