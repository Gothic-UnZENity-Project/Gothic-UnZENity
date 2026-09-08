using System.IO;
using UberLogger;
using ILogger = UberLogger.ILogger;

namespace Gothic.Core.Logging
{
    /// <summary>
    /// UberLogger sink writing the RFC 5424 style log lines into a file.
    ///
    /// Used by <see cref="FileLoggingHandler"/> for shipping builds and by the functional test harness inside the
    /// Editor. Both therefore produce the very same log format. (ADR-0001 §3.3)
    /// </summary>
    public class FileLoggingLogger : ILogger
    {
        private readonly StreamWriter _logFileWriter;
        private readonly bool _includeCallStacks;

        public FileLoggingLogger(string fileLogPath, bool includeCallStacks)
        {
            _includeCallStacks = includeCallStacks;
            _logFileWriter = new StreamWriter(fileLogPath, false);
            _logFileWriter.AutoFlush = true;
        }

        public void Log(LogInfo logInfo)
        {
            lock(this)
            {
                // RFC 5424: <timestamp> <severity> [<category>] <message>
                var fullMessage = string.Format("{0} {1} [{2}] {3}",
                    logInfo.GetRelativeTimeStampAsString(),
                    logInfo.Severity,
                    logInfo.Channel,
                    logInfo.Message
                );

                _logFileWriter.WriteLine(fullMessage);

                if(_includeCallStacks && logInfo.Callstack.Count>0)
                {
                    foreach(var frame in logInfo.Callstack)
                    {
                        _logFileWriter.WriteLine(frame.GetFormattedMethodNameWithFileName());
                    }
                    _logFileWriter.WriteLine();
                }
            }
        }

        /// <summary>
        /// Write a single line without additional formatting or checks.
        /// </summary>
        public void WriteLine(string message)
        {
            lock(this)
            {
                _logFileWriter.WriteLine(message);
            }
        }

        /// <summary>
        /// Close the file handle deterministically. Needed whenever a logger is short-lived (i.e. one per test
        /// session) and we can't wait for the finalizer to release the file.
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                _logFileWriter.Close();
            }
        }

        ~FileLoggingLogger()
        {
            lock (this)
            {
                _logFileWriter.Close();
            }
        }
    }
}
