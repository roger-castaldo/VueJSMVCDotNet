using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify an arguement of a method that cannot be set to null (this is used where the property type cannot be identified as nullable or not properly like a string)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =false)]
    public  class NotNullArguement : Attribute
    {
        private string[] _names;
        internal bool IsParameterNullable(ParameterInfo par)
        {
            return !_names.Contains(par.Name);
        }

        /// <summary>
        /// Define an arguement for a method that cannot be null
        /// </summary>
        /// <param name="name">the parameter name that cannot be null</param>
        public NotNullArguement(string name)
            : this(new string[] { name }) { }

        /// <summary>
        /// Define arguements for a method that cannot be null
        /// </summary>
        /// <param name="names">the parameter names that cannot be null</param>
        public NotNullArguement(string[] names)
        {
            _names = names;
        }
    }
}
