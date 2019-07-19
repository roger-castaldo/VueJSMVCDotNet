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
            LogMessage(LogLevels.Trace, message);
        }

        public static void Debug(string message)
        {
            LogMessage(LogLevels.Debug, message);
        }

        public static void Error(string message)
        {
            LogMessage(LogLevels.Critical, message);
        }

        private static void LogMessage(LogLevels level, string message)
        {
            if (_writer != null)
            {
                if ((int)_writer.LogLevel >= (int)level)
                    _writer.WriteLogMessage(DateTime.Now, level, message);
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
