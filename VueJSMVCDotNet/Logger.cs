using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal sealed class LogInstance : ILog
    {
        private readonly ILogWriter _writer;

        public LogInstance(ILogWriter writer)
        {
            _writer=writer;
        }

        public void Debug(string message) => this.Debug(message, null);

        public void Debug(string message, object[] pars) => this.LogMessage(LogLevels.Debug, message, pars);

        public void Error(string message) => this.Error(message, null);

        public void Error(string message, object[] pars) => this.LogMessage(LogLevels.Critical, message, pars);

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

        public void Trace(string message) => Trace(message, null);

        public void Trace(string message, object[] pars) => this.LogMessage(LogLevels.Trace, message, pars);

        private void LogMessage(LogLevels level, string message, object[] pars)
        {
            if (_writer != null&&(int)_writer.LogLevel >= (int)level)
                _writer.WriteLogMessage(DateTime.Now, level, (pars==null ? message : string.Format(message, pars)));
        }
    }

    internal static class Logger
    {
        private static LogInstance _instance;

        public static void Setup(ILogWriter writer)
        {
            _instance=new LogInstance(writer);
        }

        public static void Trace(string message)
        {
            Trace(message,null);
        }

        public static void Trace(string message,object[] pars)
        {
            _instance.Trace(message,pars);
        }

        public static void Debug(string message)
        {
            Debug(message, null);
        }

        public static void Debug(string message,object[] pars)
        {
            _instance.Debug(message,pars);
        }

        public static void Error(string message)
        {
            Error(message, null);
        }

        public static void Error(string message,object[] pars)
        {
            _instance.Error(message,pars);
        }

        public static void LogError(Exception error)
        {
            ((ILog)_instance).Error(error);
        }

        internal static object Instance
        {
            get
            {
                return _instance;
            }
        }
    }
}
