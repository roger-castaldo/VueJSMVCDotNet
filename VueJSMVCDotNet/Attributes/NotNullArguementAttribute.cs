namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify an arguement of a method that cannot be set to null (this is used where the property type cannot be identified as nullable or not properly like a string)
    /// </summary>
    /// <remarks>
    /// Define arguements for a method that cannot be null
    /// </remarks>
    /// <param name="names">the parameter names that cannot be null</param>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NotNullArguementAttribute(string[] names) : Attribute
    {
        private readonly IEnumerable<string> _names = names;
        internal bool IsParameterNullable(ParameterInfo par)
        {
            return !_names.Contains(par.Name);
        }

        /// <summary>
        /// Define an arguement for a method that cannot be null
        /// </summary>
        /// <param name="name">the parameter name that cannot be null</param>
        public NotNullArguementAttribute(string name)
            : this([name]) { }
    }
}
