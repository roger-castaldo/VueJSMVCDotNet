using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal static class Logger
    {
        private static ILogWriter _writer;

        public static void Setup(ILogWriter writer)
        {
            _writer = writer;
        }

        public static void Destroy()
        {
            _writer = null;
        }

        public static void Trace(string message)
        {
            Trace(message, null);
        }

        public static void Trace(string message,object[] pars)
        {
            LogMessage(LogLevels.Trace, message,pars);
        }

        public static void Debug(string message)
        {
            Debug(message, null);
        }

        public static void Debug(string message,object[] pars)
        {
            LogMessage(LogLevels.Debug, message,pars);
        }

        public static void Error(string message)
        {
            Error(message, null);
        }

        public static void Error(string message,object[] pars)
        {
            LogMessage(LogLevels.Critical, message,pars);
        }

        private static void LogMessage(LogLevels level, string message,object[] pars)
        {
            if (_writer != null)
            {
                if ((int)_writer.LogLevel >= (int)level)
                    _writer.WriteLogMessage(DateTime.Now, level, (pars==null ? message : string.Format(message,pars)));
            }
        }

        public static void LogError(Exception error)
        {
            while (error != null)
            {
                Error(error.Message);
                Error(error.Source);
                Error(error.StackTrace);
                error = error.InnerException;
            }
        }
    }
}
