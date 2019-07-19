using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to specify the route(path) to use for accessing the model
     */
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelRoute : Attribute
    {
        private string _host;
        public string Host
        {
            get { return _host; }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
        }

        public ModelRoute(string host, string path)
        {
            _path = path;
            _host = host;
        }

        public ModelRoute(string path)
        {
            _path = path;
            _host = "*";
        }
    }
}
