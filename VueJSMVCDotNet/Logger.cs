using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace VueJSMVCDotNet
{
    internal class Logger : ILog
    {
        private readonly ILogger _writer;

        public Logger(ILogger writer)
        {
            _writer=writer;
        }
        public void Debug(string message, params object[] pars) => this.LogMessage(LogLevel.Debug, message, pars);

        public void Error(string message, params object[] pars) => this.LogMessage(LogLevel.Critical, message, pars);

        void ILog.Error(Exception error)
        {
            while (error != null)
            {
                Error(error.Message);
                Error(error.Source);
                Error(error.StackTrace);
                error = error.InnerException;
            }
        }

        public void Trace(string message, params object[] pars) => this.LogMessage(LogLevel.Trace, message, pars);

        private void LogMessage(LogLevel level, string message, params object[] pars)
        {
            if (_writer!=null)
                _writer.Log(level, message, pars);
        }
    }
}
