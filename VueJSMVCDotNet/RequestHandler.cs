using Org.Reddragonit.VueJSMVCDotNet.Handlers;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    public static class RequestHandler
    {
        //how to startup the system as per their names, either disable invalid models or throw 
        //and exception about them
        public enum StartTypes
        {
            DisableInvalidModels,
            ThrowInvalidExceptions
        }

        public enum RequestMethods
        {
            GET,
            PUT,
            DELETE,
            PATCH,
            METHOD,
            SMETHOD
        }

        //houses a list of invalid models if StartTypes.DisableInvalidModels is passed for a startup parameter
        private static List<Type> _invalidModels;
        internal static bool IsTypeAllowed(Type type)
        {
            return !_invalidModels.Contains(type);
        }
        private static StartTypes _startType = StartTypes.DisableInvalidModels;
        //flag used to indicate if the handler is running
        private static bool _running;
        public static bool Running
        {
            get { return _running; }
        }

        private static readonly IRequestHandler[] _Handlers = new IRequestHandler[]
        {
            new JSHandler(),
            new LoadAllHandler(),
            new StaticMethodHandler(),
            new LoadHandler(),
            new UpdateHandler(),
            new SaveHandler(),
            new DeleteHandler(),
            new InstanceMethodHandler(),
            new ModelListCallHandler()
        };

        public static bool HandlesRequest(Uri uri, RequestMethods method) {
            if (!_running)
                return false;
            string url = Utility.CleanURL(uri);
            foreach (IRequestHandler handler in _Handlers)
            {
                if (handler.HandlesRequest(url, method))
                    return true;
            }
            return false;
        }

        public static string HandleRequest(Uri uri, RequestHandler.RequestMethods method, string formData, out string contentType, out int responseStatus){
            string url = Utility.CleanURL(uri);
            foreach (IRequestHandler handler in _Handlers)
            {
                if (handler.HandlesRequest(url, method)) {
                    try
                    {
                        return handler.HandleRequest(url, method, formData, out contentType, out responseStatus);
                    }catch(Exception e)
                    {
                        contentType = "text/text";
                        responseStatus = 500;
                        return "Error";
                    }
                }
            }
            contentType = "text/text";
            responseStatus = 404;
            return "Not Found";
        }

        public static void Start(StartTypes startType, ILogWriter logWriter)
        {
            Logger.Setup(logWriter);
            Logger.Debug("Starting up VueJS Request Handler");
            _startType = startType;
            AssemblyAdded();
        }

        //called when a new assembly has been loaded in the case of dynamic loading, in order 
        //to rescan for all new model types and add them accordingly.
        public static void AssemblyAdded()
        {
            Utility.ClearCaches();
            foreach (IRequestHandler irh in _Handlers)
                irh.ClearCache();
            _running = false;
            List<Type> models;
            List<Exception> errors = DefinitionValidator.Validate(out _invalidModels,out models);
            if (errors.Count > 0)
            {
                Logger.Error("Backbone validation errors:");
                foreach (Exception e in errors)
                    Logger.LogError(e);
                Logger.Error("Invalid IModels:");
                foreach (Type t in _invalidModels)
                    Logger.Error(t.FullName);
            }
            if (_startType == StartTypes.ThrowInvalidExceptions && errors.Count > 0)
                throw new ModelValidationException(errors);
            foreach (IRequestHandler irh in _Handlers)
                irh.Init(models);
            _running = true;
        }
    }
}
