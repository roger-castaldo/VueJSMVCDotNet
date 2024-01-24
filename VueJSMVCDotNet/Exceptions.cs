namespace VueJSMVCDotNet
{
    internal class CallNotFoundException : Exception {
        public CallNotFoundException(string message) :
            base(message)
        { }
    }

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    public class ModelTypeException : Exception{
        /// <summary>
        /// The type of the model generating the exception
        /// </summary>
        public Type ModelType { get; private init; }

        internal ModelTypeException(Type t,string message)
            : base(message)
        {
            ModelType=t;
        }
    }

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    public class ModelTypeMethodException : ModelTypeException
    {
        /// <summary>
        /// The name of the method causing the error
        /// </summary>
        public string MethodName { get; private init; }
        internal ModelTypeMethodException(Type t,string methodName, string message)
            : base(t,message)
        {
            MethodName=methodName;
        }
    }

    /// <summary>
    /// Thrown when a call made fails the security check
    /// </summary>
    public class InsecureAccessException : Exception
    {
        internal InsecureAccessException()
            : this("Not Authorized") { }

        internal InsecureAccessException(string message)
            : base(message) { }

    }

    /// <summary>
    /// thrown when no routes to a given model were specified by attributes
    /// </summary>    
    public class NoRouteException : ModelTypeException
    {
        internal NoRouteException(Type t)
            : base(t,$"The IModel type {t.FullName} is not valid as no Model Route has been specified.") { }
    }

    /// <summary>
    /// thrown when more than one model is mapped to the same route
    /// </summary>
    public class DuplicateRouteException : Exception
    {
        /// <summary>
        /// The first of the duplicate Paths
        /// </summary>
        public string FirstPath { get; private init; }
        /// <summary>
        /// The first Model Type containing the first path
        /// </summary>
        public Type FirstModel { get; private init; }
        /// <summary>
        /// The second of the duplicate Paths
        /// </summary>
        public string SecondPath { get; private init; }
        /// <summary>
        /// The second Model Type containg the second path
        /// </summary>
        public Type SecondModel { get; private init; }

        internal DuplicateRouteException(string path1, Type type1, string path2, Type type2)
            : base($"The IModel type {type2.FullName} is not valid as its route {path2} is a duplicate for the route {path1} contained within the Model {type1.FullName}") { 
            FirstPath= path1; 
            FirstModel= type1;
            SecondPath= path2;
            SecondModel= type2;
        }
    }

    ///<summary>
    ///thrown when more than one Load method exists in a given model
    ///</summary>
    public class DuplicateLoadMethodException : ModelTypeMethodException
    {
        internal DuplicateLoadMethodException(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} is tagged as a load method when a valid load method already exists.") { }
    }

    /// <summary>
    /// thrown when more than one Load all method exists in a given model
    /// </summary>
    public class DuplicateLoadAllMethodException : ModelTypeMethodException
    {
        internal DuplicateLoadAllMethodException(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} is tagged as a load all method when a valid load all method already exists.") {        }
    }

    /// <summary>
    /// thrown when the return type of a load method is not of the model or of the models inheritance
    /// </summary>
    public class InvalidLoadMethodReturnType : ModelTypeMethodException
    {
        internal InvalidLoadMethodReturnType(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} does not return a valid type for loading.")
        {}
    }

    /// <summary>
    /// thrown when the paremeters of a load method are not valid (ie either string, or ISecureSession and a string
    /// </summary>
    public class InvalidLoadMethodArguements : ModelTypeMethodException
    {
        internal InvalidLoadMethodArguements(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} does not return a valid type for load all.")
        {}
    }


    /// <summary>
    /// thrown when the return type of a load all method is not an array or List&lt;&gt; of the model type
    /// </summary>
    public class InvalidLoadAllMethodReturnType : ModelTypeMethodException
    {
        internal InvalidLoadAllMethodReturnType(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} does not return a valid type for load all.")
        { }
    }

    /// <summary>
    /// thrown when the return type of a load all method is not either parameterless or only contains one parameter and thats ISecureSession
    /// </summary>
    public class InvalidLoadAllArguements : ModelTypeMethodException
    {
        internal InvalidLoadAllArguements(Type t, string methodName)
            : base(t,methodName,$"The IModel type {t.FullName} is not valid because the method {methodName} does not have a valid signature for a LoadAll call.")
        { }
    }

    /// <summary>
    /// thrown when no Load method is specified
    /// </summary>
    public class NoLoadMethodException : ModelTypeException
    {
        internal NoLoadMethodException(Type t)
            : base(t,$"The IModel type {t.FullName} is not valid because there is no valid load method found.  A Load method must have the attribute ModelLoadMethod() as well as be similar to public static IModel Load(string id).") { }
    }

    /// <summary>
    /// special exception designed to house all found validation exceptions
    /// </summary>
    public class ModelValidationException : Exception
    {
        /// <summary>
        /// All the exceptions found when validating the model definitions
        /// </summary>
        public List<Exception> InnerExceptions { get; private init; }

        internal ModelValidationException(List<Exception> exceptions)
            : base("Model Definition Validations have failed.")
        {
            InnerExceptions = exceptions;
        }
    }

    /// <summary>
    /// thrown when the id property of the model is tagged as block
    /// </summary>
    public class ModelIDBlockedException : ModelTypeException
    {
        internal ModelIDBlockedException(Type t)
            : base(t,$"The IModel type {t.FullName} is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
    }

    /// <summary>
    /// thrown when no empty constructor is specifed but adding the model has not been blocked
    /// </summary>
    public class NoEmptyConstructorException : ModelTypeException
    {
        internal NoEmptyConstructorException(Type t)
            : base(t,$"The IModel type {t.FullName} is not valid because it does not block adding and has no empty constructor.")
        {
        }
    }

    /// <summary>
    /// thrown when the return type for the ModelListMethod function is not valid
    /// </summary>
    public class InvalidModelListMethodReturnException : ModelTypeMethodException
    {
        internal InvalidModelListMethodReturnException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the return type for the model list method {mi.Name} is not either List<{t.FullName}> or {t.FullName}[].")
        { }
    }

    /// <summary>
    /// thrown when the path specified does not contain the proper number of method parameters
    /// </summary>
    public class InvalidModelListParameterCountException : ModelTypeMethodException
    {

        internal InvalidModelListParameterCountException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the number of parameters for the method {mi.Name} does not match the number of variables")
        {
        }
    }

    /// <summary>
    /// thrown when a parameter used for paging a model list is not a valid type of parameter
    /// </summary>
    public class InvalidModelListPageParameterTypeException : ModelTypeMethodException
    {
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter { get; private init; }
        internal InvalidModelListPageParameterTypeException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the parameter {pi.Name} in the method {mi.Name} is not a usable as a paging parameter for a ModelListMethod.")
        {
            Parameter=pi;
        }
    }

    /// <summary>
    /// thrown when the parameter to indicate the total pages in a paged model list is not an out parameter
    /// </summary>
    public class InvalidModelListPageTotalPagesNotOutException : ModelTypeMethodException
    {
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter { get; private init; }
        internal InvalidModelListPageTotalPagesNotOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the parameter {pi.Name} in the method {mi.Name} is not an out parameter which is needed to indicate the total number of pages.")
        {
            Parameter=pi;
        }
    }

    /// <summary>
    /// thrown when the parameter of a ModelListMethod is an out parameter and it is not a paged call
    /// </summary>
    public class InvalidModelListParameterOutException : ModelTypeMethodException
    {
        /// <summary>
        /// The parameter causing the error
        /// </summary>
        public ParameterInfo Parameter { get; private init; }
        internal InvalidModelListParameterOutException(Type t, MethodInfo mi, ParameterInfo pi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the parameter {pi.Name} in the method {mi.Name} is an out parameter.")
        {
            Parameter=pi;
        }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelSaveMethodException : ModelTypeMethodException
    {
        internal DuplicateModelSaveMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the ModelSaveMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelDeleteMethodException : ModelTypeMethodException
    {
        internal DuplicateModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t, mi.Name, $"The IModel type {t.FullName} is not valid because the ModelDeleteMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelUpdateMethodException : ModelTypeMethodException
    {
        internal DuplicateModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the ModelUpdateMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelSaveMethodException : ModelTypeMethodException
    {
        internal InvalidModelSaveMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public bool Save() for ModelSaveMethod.")
        { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelDeleteMethodException : ModelTypeMethodException
    {
        internal InvalidModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public bool Delete() for ModelDeleteMethod.")
        { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelUpdateMethodException : ModelTypeMethodException
    {
        internal InvalidModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public bool Update() for ModelUpdateMethod.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod will have the same javascript signature as another for a model
    /// </summary>
    public class DuplicateMethodSignatureException : ModelTypeMethodException
    {
        internal DuplicateMethodSignatureException(Type t, MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid because the method {mi.Name} has a javascript signature identical to a previously detected method of the same same.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but is not marked as slow
    /// </summary>
    public class MethodNotMarkedAsSlow : ModelTypeMethodException
    {
        internal MethodNotMarkedAsSlow(Type t,MethodInfo mi)
            : base(t,mi.Name,$"The IModel type {t.FullName} is not valid is not valid because the method {mi.Name} is using the AddItem delegate but is not marked slow.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but is not marked as slow
    /// </summary>
    public class MethodWithAddItemNotVoid : ModelTypeMethodException
    {
        internal MethodWithAddItemNotVoid(Type t, MethodInfo mi)
            : base(t, mi.Name, $"The IModel type {t.FullName} is not valid is not valid because the method {mi.Name} is using the AddItem delegate but is not void.")
        { }
    }

    /// <summary>
    /// thrown when an a slow method fails to register properly
    /// </summary>
    public class SlowMethodRegistrationFailed : Exception
    {
        internal SlowMethodRegistrationFailed()
            : base("An error occured attempting to register the slow method invocation") { }
    }

    /// <summary>
    /// thrown when a save model call fails
    /// </summary>
    public class SaveFailedException : ModelTypeMethodException
    {
        internal SaveFailedException(Type t, InjectableMethod mi)
            : base(t, mi.Name, $"The save call for the model type {t.FullName} failed."){ }
    }
}
