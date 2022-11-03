using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    /// <summary>
    /// Used to supply the additional components for the VueHandler middleware
    /// </summary>
    public class VueHandlerOptions
    {
        private readonly ISecureSessionFactory _sessionFactory;
        internal ISecureSessionFactory SessionFactory { get { return _sessionFactory; } }

        private readonly ILogWriter _logWriter;
        internal ILogWriter LogWriter { get { return _logWriter; } }
        private readonly string _baseURL;
        internal string BaseURL { get { return _baseURL; } }
        private readonly bool _ignoreInvalidModels;
        internal bool IgnoreInvalidModels { get { return _ignoreInvalidModels; } }
        private readonly IFileProvider _fileProvider;
        internal IFileProvider FileProvider { get { return _fileProvider; } }

        /// <summary>
        /// default constructor used to supply the middleware options
        /// </summary>
        /// <param name="sessionFactory">The secure session factory builder</param>
        /// <param name="logWriter">(optional)An instance of a log writer class to write the logging information to</param>
        /// <param name="baseURL">Optional: This will remap all urls provided in attributes to the base path provided (e.g. "/modules/tester/")</param>
        /// <param name="ignoreInvalidModels">Optional: If flagged as true it will ignore/disable invalid models</param>
        /// <param name="fileProvider"></param>
        public VueHandlerOptions(ISecureSessionFactory sessionFactory, ILogWriter logWriter=null, string baseURL=null,bool ignoreInvalidModels=false, IFileProvider fileProvider=null)
        {
            _sessionFactory=sessionFactory;
            _logWriter=logWriter;
            _baseURL=baseURL;
            _ignoreInvalidModels=ignoreInvalidModels;
            _fileProvider=fileProvider;
        }
    }

    /// <summary>
    /// This is the middleware defined to intercept requests coming in and handle them when necessary
    /// </summary>
    public class VueHandlerMiddleware : IDisposable
    {
        private readonly RequestDelegate _next;
        private readonly VueHandlerOptions _options;
        private readonly ModelRequestHandler _handler;
        private readonly IFileProvider _fileProvider;

        /// <summary>
        /// default constructor as per dotnet standards
        /// </summary>
        /// <param name="next">next delegate call as per dotnet standards</param>
        /// <param name="options">the supplied options for creating the middle ware</param>
        public VueHandlerMiddleware(RequestDelegate next,VueHandlerOptions options)
        {
            _next=next;
            _options=options;
            _handler = new ModelRequestHandler(_options.LogWriter,options.BaseURL,options.IgnoreInvalidModels);
            _fileProvider = options.FileProvider;
        }

        /// <summary>
        /// Disposable implementation to allow for cleanup and proper disposal
        /// </summary>
        public void Dispose()
        {
            _handler.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// the dotnet required method for produce a dependency injectable middle ware that all calls go through
        /// </summary>
        /// <param name="context">the current httpcontext</param>
        /// <returns>a task</returns>
        public async Task InvokeAsync(HttpContext context) {
            if (_handler.HandlesRequest(context))
                await _handler.ProcessRequest(context, _options.SessionFactory.ProduceFromContext(context));
            else
                await _next(context);
        }
    }

    /// <summary>
    /// Static Middleware Extension to allow for Dependency Inject to occur, allowing for app.UseVueHandler
    /// in order to cause the library to be active in the request process
    /// </summary>
    public static class VueHandlerMiddlewareExtension
    {
        /// <summary>
        /// call based on dotnet standards for middleware dependency injection
        /// </summary>
        /// <param name="builder">the application builder</param>
        /// <param name="options">the options used to define the middle ware settings</param>
        /// <returns>the application builder with the middleware setup</returns>
        public static IApplicationBuilder UseVueHandler(
            this IApplicationBuilder builder,
            VueHandlerOptions options)
        {
            return builder.UseMiddleware<VueHandlerMiddleware>(options);
        }
    }
}
