namespace VueJSMVCDotNet
{

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    public class HandlerTypeException : Exception
    {
        /// <summary>
        /// The type of the model generating the exception
        /// </summary>
        public Type HandlerType { get; private init; }

        internal HandlerTypeException(Type t, string message)
            : base(message)
        {
            HandlerType=t;
        }
    }

    /// <summary>
    /// Base for Thrown Validation Exceptions with a specific type
    /// </summary>
    public class HandlerTypeMethodException : HandlerTypeException
    {
        /// <summary>
        /// The name of the method causing the error
        /// </summary>
        public string MethodName { get; private init; }
        internal HandlerTypeMethodException(Type t, MethodInfo method, string message)
            : base(t, message)
        {
            MethodName=method.Name;
        }
    }

    /// <summary>
    /// thrown when no routes to a given model were specified by attributes
    /// </summary>    
    public class NoRouteException : HandlerTypeException
    {
        internal NoRouteException(Type t)
            : base(t, $"The IModel type {t.FullName} is not valid as no Model Route has been specified.") { }
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
            : base($"The IModel type {type2.FullName} is not valid as its route {path2} is a duplicate for the route {path1} contained within the Model {type1.FullName}")
        {
            FirstPath= path1;
            FirstModel= type1;
            SecondPath= path2;
            SecondModel= type2;
        }
    }

    /// <summary>
    /// thrown when more than one Load all method exists in a given model
    /// </summary>
    public class DuplicateLoadAllMethodException : HandlerTypeMethodException
    {
        internal DuplicateLoadAllMethodException(Type t, MethodInfo method)
            : base(t, method, $"The IModel type {t.FullName} is not valid because the method {method.Name} is tagged as a load all method when a valid load all method already exists.") { }
    }

    /// <summary>
    /// thrown when the return type of a load all method is not an array or List&lt;&gt; of the model type
    /// </summary>
    public class InvalidLoadAllMethodReturnTypeException : HandlerTypeMethodException
    {
        internal InvalidLoadAllMethodReturnTypeException(Type t, MethodInfo method)
            : base(t, method, $"The IModel type {t.FullName} is not valid because the method {method.Name} does not return a valid type for load all.")
        { }
    }

    /// <summary>
    /// thrown when the return type of a load all method is not either parameterless or only contains one parameter and thats ISecureSession
    /// </summary>
    public class InvalidLoadAllArguementsException : HandlerTypeMethodException
    {
        internal InvalidLoadAllArguementsException(Type t, MethodInfo method)
            : base(t, method, $"The IModel type {t.FullName} is not valid because the method {method.Name} does not have a valid signature for a LoadAll call.")
        { }
    }

    /// <summary>
    /// special exception designed to house all found validation exceptions
    /// </summary>
    public class ModelValidationException : Exception
    {
        /// <summary>
        /// All the exceptions found when validating the model definitions
        /// </summary>
        public IEnumerable<Exception> InnerExceptions { get; private init; }

        internal ModelValidationException(IEnumerable<Exception> exceptions)
            : base("Model Definition Validations have failed.")
        {
            InnerExceptions = exceptions;
        }
    }

    /// <summary>
    /// thrown when the id property of the model is tagged as block
    /// </summary>
    public class ModelIDBlockedException : HandlerTypeException
    {
        internal ModelIDBlockedException(Type t)
            : base(t, $"The IModel type {t.FullName} is not valid because the ID property has been tagged with ModelIgnoreProperty.") { }
    }

    /// <summary>
    /// thrown when the return type for the ModelListMethod function is not valid
    /// </summary>
    public class InvalidModelListMethodReturnException : HandlerTypeMethodException
    {
        internal InvalidModelListMethodReturnException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the return type for the model list method {mi.Name} is not either List<{t.FullName}> or {t.FullName}[].")
        { }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelSaveMethodException : HandlerTypeMethodException
    {
        internal DuplicateModelSaveMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the ModelSaveMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelDeleteMethodException : HandlerTypeMethodException
    {
        internal DuplicateModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the ModelDeleteMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    public class DuplicateModelUpdateMethodException : HandlerTypeMethodException
    {
        internal DuplicateModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the ModelUpdateMethod is specified on the method {mi.Name} as well as another method.")
        { }
    }

    /// <summary>
    /// thrown when the ModelSaveMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelSaveMethodException : HandlerTypeMethodException
    {
        internal InvalidModelSaveMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public string Save() for ModelSaveMethod.")
        { }
    }

    /// <summary>
    /// thrown when the ModelDeleteMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelDeleteMethodException : HandlerTypeMethodException
    {
        internal InvalidModelDeleteMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public bool Delete() for ModelDeleteMethod.")
        { }
    }

    /// <summary>
    /// thrown when the ModelUpdateMethod Attribute is specified more than once in the Model
    /// </summary>
    public class InvalidModelUpdateMethodException : HandlerTypeMethodException
    {
        internal InvalidModelUpdateMethodException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the method {mi.Name} is not of the pattern public bool Update() for ModelUpdateMethod.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but is not marked as slow
    /// </summary>
    public class MethodNotMarkedAsSlowException : HandlerTypeMethodException
    {
        internal MethodNotMarkedAsSlowException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid is not valid because the method {mi.Name} is using the AddItem delegate but is not marked slow.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod uses the AddItem delegate but is not marked as slow
    /// </summary>
    public class MethodWithAddItemNotVoidException : HandlerTypeMethodException
    {
        internal MethodWithAddItemNotVoidException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid is not valid because the method {mi.Name} is using the AddItem delegate but is not void.")
        { }
    }

    /// <summary>
    /// thrown when an ExposedMethod will have the same javascript signature as another for a model
    /// </summary>
    public class DuplicateMethodSignatureException : HandlerTypeMethodException
    {
        internal DuplicateMethodSignatureException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the method {mi.Name} has a javascript signature identical to a previously detected method of the same signature.")
        { }
    }

    /// <summary>
    /// thrown when an EventStream has an invalid parameter type
    /// </summary>
    public class InvalidParameterTypeForExposedMethodException : HandlerTypeMethodException
    {
        internal InvalidParameterTypeForExposedMethodException(Type t, MethodInfo mi, ParameterInfo par)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the parameter {par.Name} for the method {mi.Name} is an out which is not supported")
        {
            this.Parameter = par;
        }

        /// <summary>
        /// The parameter that caused the issue in the method
        /// </summary>
        public ParameterInfo Parameter { get; private init; }
    }

    /// <summary>
    /// thrown when an EventStream has the same call path
    /// </summary>
    public class DuplicateEventStreamException : HandlerTypeMethodException
    {
        internal DuplicateEventStreamException(Type t, MethodInfo mi)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the method {mi.Name} will have the same call path as another.")
        { }
    }

    /// <summary>
    /// thrown when an EventStream has an invalid parameter type
    /// </summary>
    public class InvalidParameterTypeForEventStreamException : HandlerTypeMethodException
    {
        internal InvalidParameterTypeForEventStreamException(Type t, MethodInfo mi, ParameterInfo par)
            : base(t, mi, $"The IModelHandler type {t.FullName} is not valid because the parameter {par.Name} for the method {mi.Name} is an unsupprted type of {par.ParameterType.FullName}")
        {
            this.Parameter = par;
        }

        /// <summary>
        /// The parameter that caused the issue in the method
        /// </summary>
        public ParameterInfo Parameter { get; private init; }
    }
}
