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

    [Serializable]
    public class InsecureAccessException : Exception
    {
        public InsecureAccessException()
            : this("Not Authorized") { }

        public InsecureAccessException(string message)
            : base(message) { }

        protected InsecureAccessException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when no routes to a given model were specified by attributes
    [Serializable]
    public class NoRouteException : Exception
    {
        public NoRouteException(Type t)
            : base("The IModel type " + t.FullName + " is not valid as no Model Route has been specified.") { }

        protected NoRouteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when more than one model is mapped to the same route
    [Serializable]
    public class DuplicateRouteException : Exception
    {
        public DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base("The IModel type " + type2.FullName + " is not valid as its route " + path2 + " is a duplicate for the route " + path1 + " contained within the Model " + type1.FullName) { }

        protected DuplicateRouteException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when more than one Load method exists in a given model
    [Serializable]
    public class DuplicateLoadMethodException : Exception
    {
        public DuplicateLoadMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load method when a valid load method already exists.") { }

        protected DuplicateLoadMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when more than one Load all method exists in a given model
    [Serializable]
    public class DuplicateLoadAllMethodException : Exception
    {
        public DuplicateLoadAllMethodException(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load all method when a valid load all method already exists.") { }

        protected DuplicateLoadAllMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the return type of a load method is not of the model or of the models inheritance
    [Serializable]
    public class InvalidLoadMethodReturnType : Exception
    {
        public InvalidLoadMethodReturnType(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for loading.")
        { }

        protected InvalidLoadMethodReturnType(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the paremeters of a load method are not valid (ie either string, or ISecureSession and a string
    [Serializable]
    public class InvalidLoadMethodArguements : Exception
    {
        public InvalidLoadMethodArguements(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
        { }

        protected InvalidLoadMethodArguements(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    

    //thrown when the return type of a load all method is not an array or List<> of the model type
    [Serializable]
    public class InvalidLoadAllMethodReturnType : Exception
    {
        public InvalidLoadAllMethodReturnType(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
        { }

        protected InvalidLoadAllMethodReturnType(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the return type of a load all method is not either parameterless or only contains one parameter and thats ISecureSession
    [Serializable]
    public class InvalidLoadAllArguements : Exception
    {
        public InvalidLoadAllArguements(Type t, string methodName)
            : base("The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not have a valid signature for a LoadAll call.")
        { }

        protected InvalidLoadAllArguements(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when no Load method is specified
    [Serializable]
    public class NoLoadMethodException : Exception
    {
        public NoLoadMethodException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }

        protected NoLoadMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //special exception designed to house all found validation exceptions
    [Serializable]
    public class ModelValidationException : Exception
    {
        private List<Exception> _innerExceptions;
        public List<Exception> InnerExceptions
        {
            get { return _innerExceptions; }
        }

        public ModelValidationException(List<Exception> exceptions)
            : base("Model Definition Validations have failed.")
        {
            _innerExceptions = exceptions;
        }

        protected ModelValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the id property of the model is tagged as block
    [Serializable]
    public class ModelIDBlockedException : Exception
    {
        public ModelIDBlockedException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }

        protected ModelIDBlockedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when no empty constructor is specifed but adding the model has not been blocked
    [Serializable]
    public class NoEmptyConstructorException : Exception
    {
        public NoEmptyConstructorException(Type t)
            : base("The IModel type " + t.FullName + " is not valid because it does not block adding and has no empty constructor.")
        {
        }

        protected NoEmptyConstructorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the return type for the ModelListMethod function is not valid
    [Serializable]
    public class InvalidModelListMethodReturnException : Exception
    {
        public InvalidModelListMethodReturnException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the return type for the model list method " + mi.Name + " is not either List<" + t.FullName + "> or " + t.FullName + "[].")
        { }

        protected InvalidModelListMethodReturnException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the path specified does not contain the proper number of method parameters
    [Serializable]
    public class InvalidModelListParameterCountException : Exception
    {
        public InvalidModelListParameterCountException(Type t, MethodInfo mi, string path)
            : base("The IModel type " + t.FullName + " is not valid because the number of parameters for the method " + mi.Name + " does not match the number of variables in the path " + path)
        { }

        protected InvalidModelListParameterCountException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the parameter of a ModelListMethod is not a usable parameter
    [Serializable]
    public class InvalidModelListParameterTypeException : Exception
    {
        public InvalidModelListParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable parameter for a ModelListMethod.")
        { }

        protected InvalidModelListParameterTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when a parameter used for paging a model list is not a valid type of parameter
    [Serializable]
    public class InvalidModelListPageParameterTypeException : Exception
    {
        public InvalidModelListPageParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable as a paging parameter for a ModelListMethod.")
        { }

        protected InvalidModelListPageParameterTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the parameter to indicate the total pages in a paged model list is not an out parameter
    [Serializable]
    public class InvalidModelListPageTotalPagesNotOutException : Exception
    {
        public InvalidModelListPageTotalPagesNotOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not an out parameter which is needed to indicate the total number of pages.")
        { }

        protected InvalidModelListPageTotalPagesNotOutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the parameter of a ModelListMethod is an out parameter and it is not a paged call
    [Serializable]
    public class InvalidModelListParameterOutException : Exception
    {
        public InvalidModelListParameterOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base("The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is an out parameter.")
        { }

        protected InvalidModelListParameterOutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when multiple ModelListMethod are delcared and 1 or more but not all are declared as paged
    [Serializable]
    public class InvalidModelListNotAllPagedException : Exception
    {
        public InvalidModelListNotAllPagedException(Type t, MethodInfo mi, string path)
            : base("The IModel type " + t.FullName + " is not valid because ModelListMethod for the path " + path + " is not marked as paged likst the others.")
        { }

        protected InvalidModelListNotAllPagedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    [Serializable]
    public class DuplicateModelSaveMethodException : Exception
    {
        public DuplicateModelSaveMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelSaveMethod is specified on the method " + mi.Name + " as well as another method.")
        { }

        protected DuplicateModelSaveMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    [Serializable]
    public class DuplicateModelDeleteMethodException : Exception
    {
        public DuplicateModelDeleteMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelDeleteMethod is specified on the method " + mi.Name + " as well as another method.")
        { }

        protected DuplicateModelDeleteMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    [Serializable]
    public class DuplicateModelUpdateMethodException : Exception
    {
        public DuplicateModelUpdateMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the ModelUpdateMethod is specified on the method " + mi.Name + " as well as another method.")
        { }

        protected DuplicateModelUpdateMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    [Serializable]
    public class InvalidModelSaveMethodException : Exception
    {
        public InvalidModelSaveMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Save() for ModelSaveMethod.")
        { }

        protected InvalidModelSaveMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    [Serializable]
    public class InvalidModelDeleteMethodException : Exception
    {
        public InvalidModelDeleteMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Delete() for ModelDeleteMethod.")
        { }

        protected InvalidModelDeleteMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    [Serializable]
    public class InvalidModelUpdateMethodException : Exception
    {
        public InvalidModelUpdateMethodException(Type t, MethodInfo mi)
            : base("The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Update() for ModelUpdateMethod.")
        { }

        protected InvalidModelUpdateMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    //thrown when an ExposedMethod will have the same javascript signature as another for a model
    [Serializable]
    public class DuplicateMethodSignatureException : Exception
    {
        public DuplicateMethodSignatureException(Type t, MethodInfo mi)
            : base(string.Format("The IModel type {0} is not valid because the method {1} has a javascript signature identical to a previously detected method of the same same.",
            t.FullName,
            mi.Name))
        { }

        protected DuplicateMethodSignatureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
