using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// Used for specifying the level of the log message coming through
    /// </summary>
    public enum LogLevels
    {
        /// <summary>
        /// Lowest level, will display lots of detail and all other levels are reported
        /// </summary>
        Trace = 2,
        /// <summary>
        /// Mid-admount of details, only critical will be reported as well
        /// </summary>
        Debug = 1,
        /// <summary>
        /// Only log errors
        /// </summary>
        Critical = 0
    }

    /// <summary>
    /// Implement this interface to allow interception of the Logging calls from the code. 
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// The method called to write a log message
        /// </summary>
        /// <param name="timestamp">The timestamp for when the message occured</param>
        /// <param name="level">The level of the message</param>
        /// <param name="message">The message</param>
        void WriteLogMessage(DateTime timestamp, LogLevels level, string message);
        /// <summary>
        /// Returns the logging level handled by this log writer
        /// </summary>
        LogLevels LogLevel { get; }
    }
}
