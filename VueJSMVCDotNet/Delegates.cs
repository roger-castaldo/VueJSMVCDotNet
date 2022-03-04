using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal delegate bool IsValidCall(Type t, MethodInfo method, ISecureSession session,IModel model,string url,Hashtable parameters);

    /*
     * This delegate is used as a variable in an exposed method, this allows it to supply an array result in chunks of single entries 
     * at a time for methods that take a long time to generate their reponse.
     */
    public delegate void AddItem(object item, bool isLast);
}
