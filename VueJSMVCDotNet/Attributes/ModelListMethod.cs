namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to create custom collection list of a method.  Created through calling the function
    /// by its name in the code.The return is a List&lt;Type&gt; or Type[] where Type is the IModel class.  
    /// When using paging, you must add the paramters int startIndex, int pageSize, out int totalPages
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelListMethod : Attribute
    {
        internal bool Paged { get; private init; }
        /// <summary>
        /// Constructor to tag a model listing method
        /// </summary>
        /// <param name="paged">Indicates wheter or not the list is paged</param>
        public ModelListMethod(bool paged=false)
        {
            Paged = paged;
        }

    }
}
