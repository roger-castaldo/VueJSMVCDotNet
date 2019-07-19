using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * Used to create custom collection list of a method.  Created through calling the function
     * by its name in the code.  The return is a List<Type> or Type[] where Type is the IModel class.  
     * When using paging, you must add the paramters int startIndex, int pageSize, out int totalPages
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelListMethod : Attribute
    {
        private string _path;
        public string Path
        {
            get { return _path; }
        }

        private bool _paged;
        public bool Paged
        {
            get { return _paged; }
        }

        public ModelListMethod(string path, bool paged)
        {
            _path = path;
            _paged = paged;
        }

        public ModelListMethod(string path)
            : this(path, false)
        { }

    }
}
