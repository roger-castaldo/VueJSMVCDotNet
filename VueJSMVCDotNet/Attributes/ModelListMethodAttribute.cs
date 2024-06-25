namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to create custom collection list of a method.  Created through calling the function
    /// by its name in the code.The return is a List&lt;Type&gt; or Type[] where Type is the IModel class.  
    /// When using paging, you must add the paramters int startIndex, int pageSize, out int totalPages
    /// </summary>
    /// <remarks>
    /// Constructor to tag a model listing method
    /// </remarks>
    /// <param name="paged">Indicates wheter or not the list is paged</param>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ModelListMethodAttribute(bool paged = false) : Attribute
    {
        internal bool Paged => paged;
    }
}
