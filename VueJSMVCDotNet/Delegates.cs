using VueJSMVCDotNet.Interfaces;
using System.Collections;

namespace VueJSMVCDotNet
{
    internal delegate bool IsValidCall(Type t, MethodInfo method, ISecureSession session,IModel model,string url,Hashtable parameters);

    /// <summary>
    /// This delegate is used as a variable in an exposed method, this allows it to supply an array result in chunks of single entries 
    /// at a time for methods that take a long time to generate their reponse.
    /// </summary>
    /// <param name="item">The item to add to the output array</param>
    /// <param name="isLast">set true for when the last item has been added</param>
    public delegate void AddItem(object item, bool isLast);
}
