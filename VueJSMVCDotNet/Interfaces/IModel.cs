using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    /*
     * The core interface for the whole system, that being a model.  Implements basic functions and 
     * the id property.  Those are all the required minimums for a Model, as well as using this interface it allows
     * all models to be loaded by finding classes that implement this interface.
     */
    public interface IModel
    {
        string id { get; }
    }
}
