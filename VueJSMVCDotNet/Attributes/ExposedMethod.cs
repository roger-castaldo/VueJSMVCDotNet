using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Attributes
{
    /*
     * This attribute is used to expose a method to a javascript based called for a model. A static
     * call will be attached to the Model object, whereas a non-static call will be attached to an 
     * instance of the model and is used to perform operations on the model.  Allow null response is used 
     * to indicate that the function can respond with null, otherwise null response is treated as an error.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ExposedMethod : Attribute
    {
        private bool _allowNullResponse;
        public bool AllowNullResponse
        {
            get { return _allowNullResponse; }
        }

        private bool _isSlow;
        public bool IsSlow { get { return _isSlow; } }

        private Type _arrayElementType;
        public Type ArrayElementType { get { return _arrayElementType; } }


        public ExposedMethod() :
            this(false,false,null)
        { }

        public ExposedMethod(bool allowNullResponse)
            : this(allowNullResponse, false, null) { }

        public ExposedMethod(bool allowNullResponse, bool isSlow)
            : this(allowNullResponse, isSlow, null) { }

        public ExposedMethod(bool allowNullResponse, Type arrayElementType)
            : this(allowNullResponse, true,arrayElementType) { }

        private ExposedMethod(bool allowNullResponse,bool isSlow,Type arrayElementType)
        {
            _allowNullResponse = allowNullResponse;
            _isSlow = isSlow;
            _arrayElementType = arrayElementType;
        }
    }
}
