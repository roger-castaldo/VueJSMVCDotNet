using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    public enum LogLevels
    {
        Trace = 2,
        Debug = 1,
        Critical = 0
    }

    /*
     * Implement this interface to allow interception of the Logging calls from the code.
     */
    public interface ILogWriter
    {
        void WriteLogMessage(DateTime timestamp, LogLevels level, string message);
        LogLevels LogLevel { get; }
    }
}
