﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Handlers;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    /// <summary>
    /// Used to supply the additional components for the VueModelsHandler
    /// </summary>
    public class VueModelsOptions
    {

        private readonly ISecureSessionFactory _sessionFactory;
        internal ISecureSessionFactory SessionFactory =>_sessionFactory;
        private readonly string _baseURL;
        internal string BaseURL => _baseURL; 
        private readonly bool _ignoreInvalidModels;
        internal bool IgnoreInvalidModels => _ignoreInvalidModels;
        private readonly string _coreJSURL;
        internal string CoreJSURL => _coreJSURL;
        private readonly string _coreJSImport;
        internal string CoreJSImport => _coreJSImport;
        private readonly string[] _securityHeaders;
        internal string[] SecurityHeaders => _securityHeaders;


        /// <summary>
        /// default constructor used to supply the middleware options
        /// </summary>
        /// <param name="sessionFactory">The secure session factory builder</param>
        /// <param name="baseURL">Optional: This will remap all urls provided in attributes to the base path provided (e.g. "/modules/tester/")</param>
        /// <param name="ignoreInvalidModels">Optional: If flagged as true it will ignore/disable invalid models</param>
        /// <param name="coreJSURL">Optional: This will remap the core JS url that is imported by all classes to a different path</param>
        /// <param name="coreJSImport">Optional:  This will change the import call from the core js url to the module name</param>
        /// <param name="securityHeaders">Optional:  A list of header keys to read from and write to for all requests if using headers for security</param>
        public VueModelsOptions(ISecureSessionFactory sessionFactory, string baseURL=null,bool ignoreInvalidModels=false,string coreJSURL=JSHandler.CORE_URL,string coreJSImport = null, string[] securityHeaders=null)
        {
            _sessionFactory=sessionFactory;
            _baseURL=baseURL;
            _ignoreInvalidModels=ignoreInvalidModels;
            _coreJSURL=coreJSURL;
            _coreJSImport=coreJSImport;
            _securityHeaders=securityHeaders;
        }
    }

    /// <summary>
    /// Used to supply the additional components for the MessagesHandler
    /// </summary>
    /// 
    public class MessageHandlerOptions
    {
        private readonly string _baseURL;
        internal string BaseURL { get { return _baseURL; } }

        /// <summary>
        /// Constructor for specifying the components required to use the MessageHandler
        /// </summary>
        /// <param name="baseURL">The base url for the messages to exist inside</param>
        public MessageHandlerOptions(string baseURL)
        {
            _baseURL= baseURL;
        }
    }

    /// <summary>
    /// Used to supply the additional components for the VueFilesHandler
    /// </summary>
    /// 
    public class VueFilesHandlerOptions
    {
        
        private readonly string _baseURL;
        internal string BaseURL { get { return _baseURL; } }

        /// <summary>
        /// Constructor for specifying the components required to use the MessageHandler
        /// </summary>
        /// <param name="baseURL">The base url for the messages to exist inside</param>
        public VueFilesHandlerOptions(string baseURL)
        {
            _baseURL= baseURL;
        }
    }

    /// <summary>
    /// Used to supply the additional configurations for the different middleware components
    /// </summary>
    public class VueMiddlewareOptions
    {
        private readonly ILogWriter _logWriter;
        internal ILogWriter LogWriter { get { return _logWriter; } }
        private readonly string _vueImportPath;
        internal string VueImportPath { get { return _vueImportPath; } }
        private readonly string _vueLoaderImportPath;
        internal string VueLoaderImportPath { get { return _vueLoaderImportPath; } }
        private readonly IFileProvider _fileProvider;
        internal IFileProvider FileProvider { get { return _fileProvider; } }
        private readonly VueModelsOptions _modelsOptions;
        internal VueModelsOptions VueModelsOptions { get { return _modelsOptions; } }

        private readonly MessageHandlerOptions _messageOptions;
        internal MessageHandlerOptions MessageOptions { get { return _messageOptions; } }
        private readonly VueFilesHandlerOptions _vueFilesOptions;
        internal VueFilesHandlerOptions VueFilesOptions { get { return _vueFilesOptions; } }

        private VueMiddleware _middleWare;
        internal VueMiddleware VueMiddleware { set { _middleWare = value; } }

        /// <summary>
        /// The constructor used to build the options for the middle ware components
        /// </summary>
        /// <param name="modelsOptions">Optional: must be provided in order to use the IModel and autogenerated Models/Rest interfaces</param>
        /// <param name="messageOptions">Optional: to be provided if the message translator component is to be used</param>
        /// <param name="logWriter">(optional)An instance of a log writer class to write the logging information to</param>
        /// <param name="fileProvider">(Optional) An instance of a file provider, required if using VueFiles or Messages</param>
        /// <param name="vueFilesOptions">(Optional) Settings to be used file the Vue File handler</param>
        /// <param name="vueImportPath">(Optional) The import path for the VueJs library: default="https://unpkg.com/vue@3/dist/vue.runtime.esm-browser.prod.js"</param>
        /// <param name="vueLoaderImportPath">(Optional) The import path for the Vue-Loader library: default="https://unpkg.com/vue3-sfc-loader@0.8.4/dist/vue3-sfc-loader.esm.js"</param>
        public VueMiddlewareOptions(VueModelsOptions modelsOptions=null, IFileProvider fileProvider = null, MessageHandlerOptions messageOptions=null,ILogWriter logWriter=null,string vueImportPath=null,string vueLoaderImportPath=null,VueFilesHandlerOptions vueFilesOptions=null)
        {
            _modelsOptions=modelsOptions;
            _messageOptions=messageOptions;
            _logWriter=logWriter;
            _vueImportPath=(vueImportPath==null ? "https://unpkg.com/vue@3/dist/vue.runtime.esm-browser.prod.js" : vueImportPath);
            _vueFilesOptions=vueFilesOptions;
            _vueLoaderImportPath=(vueLoaderImportPath==null ? "https://unpkg.com/vue3-sfc-loader@0.8.4/dist/vue3-sfc-loader.esm.js" : vueLoaderImportPath);
            _fileProvider=fileProvider;
            if ((vueFilesOptions!=null||messageOptions!=null) && fileProvider==null)
                throw new ArgumentNullException("fileProvider");
        }

        /// <summary>
        /// called when an assemblyloadcontext needs to be unloaded, this will remove all references to 
        /// that load context to allow for an unload
        /// </summary>
        /// <param name="context">The assembly context being unloaded</param>
        public void UnloadAssemblyContext(AssemblyLoadContext context){
            UnloadAssemblyContext(context.Name);
        }

        /// <summary>
        /// called when an assembly context needs to be unloaded without providing the context but its name
        /// instead
        /// </summary>
        /// <param name="contextName">The name of the assembly load context to unload</param>
        public void UnloadAssemblyContext(string contextName){
            _middleWare.UnloadAssemblyContext(contextName);
        }

        /// <summary>
        /// Called when a new Assembly Load Context has been added
        /// </summary>
        /// <param name="contextName">The name of the context that was added</param>
        public void AsssemblyLoadContextAdded(string contextName){
            _middleWare.AsssemblyLoadContextAdded(contextName);
        }

        /// <summary>
        /// Called when a new Assembly Load Context has been added
        /// </summary>
        /// <param name="alc">The assembly load context that was added</param>
        /// <exception cref="ModelValidationException">Houses a set of exceptions if any newly loaded models fail validation</exception>
        public void AsssemblyLoadContextAdded(AssemblyLoadContext alc){
            AsssemblyLoadContextAdded(alc.Name);
        }

        ///<summary>
        ///called when a new assembly has been loaded in the case of dynamic loading, in order 
        ///to rescan for all new model types and add them accordingly.
        ///</summary>
        public void AssemblyAdded()
        {
            _middleWare.AssemblyAdded();
        }
    }

    /// <summary>
    /// This is the middleware defined to intercept requests coming in and handle them when necessary
    /// </summary>
    public class VueMiddleware : IDisposable
    {
        private readonly VueMiddlewareOptions _options;
        /// <summary>
        /// The Options that were supplied to construct the VueMiddleware
        /// </summary>
        public VueMiddlewareOptions Options => _options;
        private readonly ModelRequestHandler _modelHandler;
        private readonly MessagesHandler _messageHandler;
        private readonly VueFilesHandler _vueFileHandler;

        /// <summary>
        /// default constructor as per dotnet standards
        /// </summary>
        /// <param name="next">next delegate call as per dotnet standards</param>
        /// <param name="options">the supplied options for creating the middle ware</param>
        public VueMiddleware(RequestDelegate next, VueMiddlewareOptions options)
        {
            options.VueMiddleware=this;
            _options = options;
            next = (next==null ? new RequestDelegate(NotFound) : next);
            if (options.VueFilesOptions!=null)
            {
                _vueFileHandler = new VueFilesHandler(options.FileProvider, options.VueFilesOptions.BaseURL, options.LogWriter, options.VueImportPath, options.VueLoaderImportPath, next);
                next = new RequestDelegate(_vueFileHandler.ProcessRequest);
            }
            if (options.MessageOptions!=null)
            {
                _messageHandler = new MessagesHandler(options.FileProvider, options.MessageOptions.BaseURL, options.LogWriter, next);
                next = new RequestDelegate(_messageHandler.ProcessRequest);
            }
            if (options.VueModelsOptions!=null)
            {
                _modelHandler = new ModelRequestHandler(options.LogWriter, options.VueModelsOptions.BaseURL, options.VueModelsOptions.IgnoreInvalidModels, options.VueImportPath,
                    options.VueModelsOptions.CoreJSURL, options.VueModelsOptions.CoreJSImport,options.VueModelsOptions.SecurityHeaders,
                options.VueModelsOptions.SessionFactory,next);
            }
        }

        /// <summary>
        /// Disposable implementation to allow for cleanup and proper disposal
        /// </summary>
        public void Dispose()
        {
            if (_modelHandler!=null)
                _modelHandler.Dispose();
            if (_messageHandler!=null)
                _messageHandler.Dispose();
            if (_vueFileHandler!=null)
                _vueFileHandler.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// the dotnet required method for produce a dependency injectable middle ware that all calls go through
        /// </summary>
        /// <param name="context">the current httpcontext</param>
        /// <returns>a task</returns>
        public async Task InvokeAsync(HttpContext context) {
            if (_modelHandler!=null)
                await _modelHandler.ProcessRequest(context);
            else if (_messageHandler!=null)
                await _messageHandler.ProcessRequest(context);
            else
                await _vueFileHandler.ProcessRequest(context);
        }

        private async Task NotFound(HttpContext context)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Not Found");
        }
       internal void UnloadAssemblyContext(string contextName){
            if (_modelHandler!=null){
                _modelHandler.UnloadAssemblyContext(contextName);
            }
        }

        internal void AssemblyAdded(){
            if (_modelHandler!=null){
                _modelHandler.AssemblyAdded();
            }
        }

        internal void AsssemblyLoadContextAdded(string contextName){
            if (_modelHandler!=null){
                _modelHandler.AsssemblyLoadContextAdded(contextName);
            }
        }
    }

    /// <summary>
    /// Static Middleware Extension to allow for Dependency Inject to occur, allowing for app.UseVueHandler
    /// in order to cause the library to be active in the request process
    /// </summary>
    [ExcludeFromCodeCoverage()]
    public static class VueMiddlewareExtension
    {
        /// <summary>
        /// call based on dotnet standards for middleware dependency injection
        /// </summary>
        /// <param name="builder">the application builder</param>
        /// <param name="options">the options used to define the middle ware settings</param>
        /// <returns>the application builder with the middleware setup</returns>
        public static IApplicationBuilder UseVueMiddleware(
            this IApplicationBuilder builder,
            VueMiddlewareOptions options)
        {
            return builder.UseMiddleware<VueMiddleware>(options);
        }
    }
}
