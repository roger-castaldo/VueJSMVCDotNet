using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// Used to pass through the internal logging mechanism used by the MiddleWare
    /// to easily allow for logging calls to be made within call backs
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Log a trace level message
        /// </summary>
        /// <param name="message">the log message</param>
        void Trace(string message);

        /// <summary>
        /// Log a trace level message with a formatted string
        /// </summary>
        /// <param name="message">the formatted log message</param>
        /// <param name="pars">the object[] to pass to string.format</param>
        void Trace(string message, object[] pars);

        /// <summary>
        /// Log a debug leve message
        /// </summary>
        /// <param name="message">the log message</param>
        void Debug(string message);

        /// <summary>
        /// Log a debug level message
        /// </summary>
        /// <param name="message">the log message</param>
        /// <param name="pars">the object[] to pass to string.format</param>
        void Debug(string message, object[] pars);

        /// <summary>
        /// Log a error level message
        /// </summary>
        /// <param name="message">the log message</param>
        void Error(string message);

        /// <summary>
        /// Log a error level message
        /// </summary>
        /// <param name="message">the log message</param>
        /// <param name="pars">the object[] to pass to string.format</param>
        void Error(string message, object[] pars);

        /// <summary>
        /// Log a error level message
        /// </summary>
        /// <param name="error">the exception to log</param>
        void Error(Exception error);
    }
}
