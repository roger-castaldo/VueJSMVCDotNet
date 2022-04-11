using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class CallNotFoundException:Exception{
        public CallNotFoundException() :
            this("Not Found")
        { }

        public CallNotFoundException(string message) :
            base(message)
        { }
    }

    /// <summary>
    /// Thrown when a call made fails the security check
    /// </summary>
    [Serializable]
    public class InsecureAccessException : Exception
    {
        internal InsecureAccessException()
            : this("Not Authorized") { }

        internal InsecureAccessException(string message)
            : base(message) { }

        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InsecureAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when no routes to a given model were specified by attributes
    /// </summary>
    [Serializable]
    public class NoRouteException : Exception
    {
        internal NoRouteException(Type t)
            : base("The IModel type " + t.FullName + " is not valid as no Model Route has been specified.") { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NoRouteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when more than one model is mapped to the same route
    /// </summary>
    [Serializable]
    public class DuplicateRouteException : Exception
    {
        internal DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base("The IModel type " + type2.FullName + " is not valid as its route " + path2 + " is a duplicate for the route " + path1 + " contained within the Model " + type1.FullName) { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateRouteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    ///<summary>
    ///thrown when more than one Load method exists in a given model
    ///</summary>
    [Serializable]
    public class DuplicateLoadMethodException : Exception
    {
        internal DuplicateLoadMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load method when a valid load method already exists.") { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateLoadMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when more than one Load all method exists in a given model
    /// </summary>
    [Serializable]
    public class DuplicateLoadAllMethodException : Exception
    {
        internal DuplicateLoadAllMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load all method when a valid load all method already exists.") { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateLoadAllMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the return type of a load method is not of the model or of the models inheritance
    /// </summary>
    [Serializable]
    public class InvalidLoadMethodReturnType : Exception
    {
        internal InvalidLoadMethodReturnType(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for loading.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLoadMethodReturnType(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the paremeters of a load method are not valid (ie either string, or ISecureSession and a string
    /// </summary>
    [Serializable]
    public class InvalidLoadMethodArguements : Exception
    {
        internal InvalidLoadMethodArguements(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLoadMethodArguements(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// thrown when the return type of a load all method is not an array or List&lt;&gt; of the model type
    /// </summary>
    [Serializable]
    public class InvalidLoadAllMethodReturnType : Exception
    {
        internal InvalidLoadAllMethodReturnType(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLoadAllMethodReturnType(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the return type of a load all method is not either parameterless or only contains one parameter and thats ISecureSession
    /// </summary>
    [Serializable]
    public class InvalidLoadAllArguements : Exception
    {
        internal InvalidLoadAllArguements(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not have a valid signature for a LoadAll call.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidLoadAllArguements(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when no Load method is specified
    /// </summary>
    [Serializable]
    public class NoLoadMethodException : Exception
    {
        internal NoLoadMethodException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NoLoadMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// special exception designed to house all found validation exceptions
    /// </summary>
    [Serializable]
    public class ModelValidationException : Exception
    {
        private List<Exception> _innerExceptions;
        /// <summary>
        /// All the exceptions found when validating the model definitions
        /// </summary>
        public List<Exception> InnerExceptions
        {
            get { return _innerExceptions; }
        }

        internal ModelValidationException(List<Exception> exceptions)
            : base("Model Definition Validations have failed.")
        {
            _innerExceptions = exceptions;
        }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ModelValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the id property of the model is tagged as block
    /// </summary>
    [Serializable]
    public class ModelIDBlockedException : Exception
    {
        internal ModelIDBlockedException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ModelIDBlockedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when no empty constructor is specifed but adding the model has not been blocked
    /// </summary>
    [Serializable]
    public class NoEmptyConstructorException : Exception
    {
        internal NoEmptyConstructorException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because it does not block adding and has no empty constructor.")
        {
        }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NoEmptyConstructorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the return type for the ModelListMethod function is not valid
    /// </summary>
    [Serializable]
    public class InvalidModelListMethodReturnException : Exception
    {
        internal InvalidModelListMethodReturnException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the return type for the model list method " + mi.Name + " is not either List<" + t.FullName + "> or " + t.FullName + "[].")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListMethodReturnException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the path specified does not contain the proper number of method parameters
    /// </summary>
    [Serializable]
    public class InvalidModelListParameterCountException : Exception
    {
        internal InvalidModelListParameterCountException(Type t, MethodInfo mi, string path)
            : base("The IModel type " + t.FullName + " is not valid because the number of parameters for the method " + mi.Name + " does not match the number of variables in the path " + path)
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListParameterCountException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the parameter of a ModelListMethod is not a usable parameter
    /// </summary>
    [Serializable]
    public class InvalidModelListParameterTypeException : Exception
    {
        internal InvalidModelListParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable parameter for a ModelListMethod.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListParameterTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when a parameter used for paging a model list is not a valid type of parameter
    /// </summary>
    [Serializable]
    public class InvalidModelListPageParameterTypeException : Exception
    {
        internal InvalidModelListPageParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable as a paging parameter for a ModelListMethod.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListPageParameterTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the parameter to indicate the total pages in a paged model list is not an out parameter
    /// </summary>
    [Serializable]
    public class InvalidModelListPageTotalPagesNotOutException : Exception
    {
        internal InvalidModelListPageTotalPagesNotOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not an out parameter which is needed to indicate the total number of pages.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListPageTotalPagesNotOutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the parameter of a ModelListMethod is an out parameter and it is not a paged call
    /// </summary>
    [Serializable]
    public class InvalidModelListParameterOutException : Exception
    {
        internal InvalidModelListParameterOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is an out parameter.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListParameterOutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when multiple ModelListMethod are delcared and 1 or more but not all are declared as paged
    /// </summary>
    [Serializable]
    public class InvalidModelListNotAllPagedException : Exception
    {
        internal InvalidModelListNotAllPagedException(Type t, MethodInfo mi, string path)
            : base("The IModel type " + t.FullName + " is not valid because ModelListMethod for the path " + path + " is not marked as paged likst the others.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelListNotAllPagedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class DuplicateModelSaveMethodException : Exception
    {
        internal DuplicateModelSaveMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelSaveMethod is specified on the method " + mi.Name + " as well as another method.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateModelSaveMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class DuplicateModelDeleteMethodException : Exception
    {
        internal DuplicateModelDeleteMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelDeleteMethod is specified on the method " + mi.Name + " as well as another method.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateModelDeleteMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class DuplicateModelUpdateMethodException : Exception
    {
        internal DuplicateModelUpdateMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelUpdateMethod is specified on the method " + mi.Name + " as well as another method.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateModelUpdateMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class InvalidModelSaveMethodException : Exception
    {
        internal InvalidModelSaveMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Save() for ModelSaveMethod.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelSaveMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class InvalidModelDeleteMethodException : Exception
    {
        internal InvalidModelDeleteMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Delete() for ModelDeleteMethod.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelDeleteMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    [Serializable]
    public class InvalidModelUpdateMethodException : Exception
    {
        internal InvalidModelUpdateMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Update() for ModelUpdateMethod.")
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidModelUpdateMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when an ExposedMethod will have the same javascript signature as another for a model
    /// </summary>
    [Serializable]
    public class DuplicateMethodSignatureException : Exception
    {
        internal DuplicateMethodSignatureException(Type t, MethodInfo mi)
            : base(string.Format("The IModel type {0} is not valid because the method {1} has a javascript signature identical to a previously detected method of the same same.",
            t.FullName,
            mi.Name))
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DuplicateMethodSignatureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but is not marked as slow
    /// </summary>
    [Serializable]
    public class MethodNotMarkedAsSlow : Exception
    {
        internal MethodNotMarkedAsSlow(Type t,MethodInfo mi)
            : base(string.Format("The IModel type {0} is not valid is not valid because the method {1} is using the AddItem delegate but is not marked slow.",
            t.FullName,
            mi.Name))
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MethodNotMarkedAsSlow(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but has a return value
    /// </summary>
    [Serializable]
    public class MethodUsesAddItemNotVoid : Exception
    {
        internal MethodUsesAddItemNotVoid(Type t, MethodInfo mi)
            : base(string.Format("The IModel type {0} is not valid because the method {1} is using the AddItem delegate requires a void response.",
            t.FullName,
            mi.Name))
        { }
        /// <summary>
        /// Used to serialize the excepion when necessary
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MethodUsesAddItemNotVoid(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
