using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to create custom collection list of a method.  Created through calling the function
    /// by its name in the code.The return is a List&lt;Type&gt; or Type[] where Type is the IModel class.  
    /// When using paging, you must add the paramters int startIndex, int pageSize, out int totalPages
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelListMethod : Attribute
    {
        private string _path;
        internal string Path
        {
            get { return _path; }
        }

        private bool _paged;
        internal bool Paged
        {
            get { return _paged; }
        }

        /// <summary>
        /// Constructor to tag a model listing method
        /// </summary>
        /// <param name="path">The url to assign to the list method</param>
        /// <param name="paged">Indicates wheter or not the list is paged</param>
        public ModelListMethod(string path, bool paged=false)
        {
            _path = path;
            _paged = paged;
        }

    }
}
