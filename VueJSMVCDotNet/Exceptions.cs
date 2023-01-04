using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class CallNotFoundException : Exception {
        public CallNotFoundException() :
            this("Not Found")
        { }

        public CallNotFoundException(string message) :
            base(message)
        { }
    }

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    [Serializable]
    public class ModelTypeException : Exception{
        private Type _modelType;
        /// <summary>
        /// The type of the model generating the exception
        /// </summary>
        public Type ModelType => _modelType;

        internal ModelTypeException(Type t,string message)
            : base(message)
        {
            _modelType=t;
        }

        protected ModelTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    [Serializable]
    public class ModelTypeMethodException : ModelTypeException
    {
        private string _methodName;
        /// <summary>
        /// The name of the method causing the error
        /// </summary>
        public string MethodName => _methodName;
        internal ModelTypeMethodException(Type t,string methodName, string message)
            : base(t,message)
        {
            _methodName=methodName;
        }

        protected ModelTypeMethodException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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
    public class NoRouteException : ModelTypeException
    {
        internal NoRouteException(Type t)
            : base(t,"The IModel type " + t.FullName + " is not valid as no Model Route has been specified.") { }
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
        private string _firstPath;
        /// <summary>
        /// The first of the duplicate Paths
        /// </summary>
        public string FirstPath => _firstPath;
        private Type _firstModel;
        /// <summary>
        /// The first Model Type containing the first path
        /// </summary>
        public Type FirstModel => _firstModel;
        private string _secondPath;
        /// <summary>
        /// The second of the duplicate Paths
        /// </summary>
        public string SecondPath => _secondPath;
        private Type _secondType;
        /// <summary>
        /// The second Model Type containg the second path
        /// </summary>
        public Type SecondType => _secondType;

        internal DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base("The IModel type " + type2.FullName + " is not valid as its route " + path2 + " is a duplicate for the route " + path1 + " contained within the Model " + type1.FullName) { 
            _firstPath= path1; 
            _firstModel= type1;
            _secondPath= path2;
            _secondType= type2;
        }
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
    public class DuplicateLoadMethodException : ModelTypeMethodException
    {
        internal DuplicateLoadMethodException(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load method when a valid load method already exists.") { }
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
    public class DuplicateLoadAllMethodException : ModelTypeMethodException
    {
        internal DuplicateLoadAllMethodException(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " is tagged as a load all method when a valid load all method already exists.") {        }
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
    public class InvalidLoadMethodReturnType : ModelTypeMethodException
    {
        internal InvalidLoadMethodReturnType(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for loading.")
        {}
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
    public class InvalidLoadMethodArguements : ModelTypeMethodException
    {
        internal InvalidLoadMethodArguements(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
        {}
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
    public class InvalidLoadAllMethodReturnType : ModelTypeMethodException
    {
        internal InvalidLoadAllMethodReturnType(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not return a valid type for load all.")
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
    public class InvalidLoadAllArguements : ModelTypeMethodException
    {
        internal InvalidLoadAllArguements(Type t, string methodName)
            : base(t,methodName,"The IModel type " + t.FullName + " is not valid because the method " + methodName + " does not have a valid signature for a LoadAll call.")
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
    public class NoLoadMethodException : ModelTypeException
    {
        internal NoLoadMethodException(Type t)
            : base(t,"The IModel type " + t.FullName + " is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }
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
    public class ModelIDBlockedException : ModelTypeException
    {
        internal ModelIDBlockedException(Type t)
            : base(t,"The IModel type " + t.FullName + " is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
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
    public class NoEmptyConstructorException : ModelTypeException
    {
        internal NoEmptyConstructorException(Type t)
            : base(t,"The IModel type " + t.FullName + " is not valid because it does not block adding and has no empty constructor.")
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
    public class InvalidModelListMethodReturnException : ModelTypeMethodException
    {
        internal InvalidModelListMethodReturnException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the return type for the model list method " + mi.Name + " is not either List<" + t.FullName + "> or " + t.FullName + "[].")
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
    public class InvalidModelListParameterCountException : ModelTypeMethodException
    {
        private string _path;
        /// <summary>
        /// The path for the model list
        /// </summary>
        public string Path => _path;

        internal InvalidModelListParameterCountException(Type t, MethodInfo mi, string path)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the number of parameters for the method " + mi.Name + " does not match the number of variables in the path " + path)
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
    public class InvalidModelListParameterTypeException : ModelTypeMethodException
    {
        private ParameterInfo _parameter;
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter => _parameter;

        internal InvalidModelListParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t, mi.Name, "The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable parameter for a ModelListMethod.")
        {
            _parameter= pi;
        }
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
    public class InvalidModelListPageParameterTypeException : ModelTypeMethodException
    {
        private ParameterInfo _parameter;
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter => _parameter;
        internal InvalidModelListPageParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not a usable as a paging parameter for a ModelListMethod.")
        {
            _parameter=pi;
        }
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
    public class InvalidModelListPageTotalPagesNotOutException : ModelTypeMethodException
    {
        private ParameterInfo _parameter;
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter => _parameter;
        internal InvalidModelListPageTotalPagesNotOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is not an out parameter which is needed to indicate the total number of pages.")
        {
            _parameter=pi;
        }
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
    public class InvalidModelListParameterOutException : ModelTypeMethodException
    {
        private ParameterInfo _parameter;
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter => _parameter;
        internal InvalidModelListParameterOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the parameter " + pi.Name + " in the method " + mi.Name + " is an out parameter.")
        {
            _parameter=pi;
        }
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
    public class InvalidModelListNotAllPagedException : ModelTypeMethodException
    {
        private string _path;
        /// <summary>
        /// The path containing the error
        /// </summary>
        public string Path => _path;
        internal InvalidModelListNotAllPagedException(Type t, MethodInfo mi, string path)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because ModelListMethod for the path " + path + " is not marked as paged likst the others.")
        {
            _path=path;
        }
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
    public class DuplicateModelSaveMethodException : ModelTypeMethodException
    {
        internal DuplicateModelSaveMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the ModelSaveMethod is specified on the method " + mi.Name + " as well as another method.")
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
    public class DuplicateModelDeleteMethodException : ModelTypeMethodException
    {
        internal DuplicateModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the ModelDeleteMethod is specified on the method " + mi.Name + " as well as another method.")
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
    public class DuplicateModelUpdateMethodException : ModelTypeMethodException
    {
        internal DuplicateModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the ModelUpdateMethod is specified on the method " + mi.Name + " as well as another method.")
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
    public class InvalidModelSaveMethodException : ModelTypeMethodException
    {
        internal InvalidModelSaveMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Save() for ModelSaveMethod.")
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
    public class InvalidModelDeleteMethodException : ModelTypeMethodException
    {
        internal InvalidModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Delete() for ModelDeleteMethod.")
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
    public class InvalidModelUpdateMethodException : ModelTypeMethodException
    {
        internal InvalidModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,"The IModel type " + t.FullName + " is not valid because the method " + mi.Name + " is not of the pattern public bool Update() for ModelUpdateMethod.")
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
    public class DuplicateMethodSignatureException : ModelTypeMethodException
    {
        internal DuplicateMethodSignatureException(Type t, MethodInfo mi)
            : base(t,mi.Name,string.Format("The IModel type {0} is not valid because the method {1} has a javascript signature identical to a previously detected method of the same same.",
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
    public class MethodNotMarkedAsSlow : ModelTypeMethodException
    {
        internal MethodNotMarkedAsSlow(Type t,MethodInfo mi)
            : base(t,mi.Name,string.Format("The IModel type {0} is not valid is not valid because the method {1} is using the AddItem delegate but is not marked slow.",
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
    public class MethodUsesAddItemNotVoid : ModelTypeMethodException
    {
        internal MethodUsesAddItemNotVoid(Type t, MethodInfo mi)
            : base(t,mi.Name,string.Format("The IModel type {0} is not valid because the method {1} is using the AddItem delegate requires a void response.",
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
