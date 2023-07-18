using System;
using System.Collections.Generic;
using System.Text;

namespace VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// The core interface for the whole system, that being a model.  Implements basic functions and
    /// the id property.Those are all the required minimums for a Model, as well as using this interface it allows
    /// all models to be loaded by finding classes that implement this interface.
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// The unique id for this model instance that all model based rest calls will use
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        string id { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
