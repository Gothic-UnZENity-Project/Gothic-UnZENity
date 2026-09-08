using System.Linq;
using UberLogger;
using Object = UnityEngine.Object;

namespace Gothic.Core.Logging
{
    /// <summary>
    /// Restricts what <see cref="FileLoggingLogger"/> receives to a minimum severity and an optional
    /// <see cref="LogCat"/> allow list. An empty category list means "every category".
    /// </summary>
    public class FileLoggingFilter : IFilter
    {
        private readonly LogSeverity _logLevel;
        private readonly string[] _logCategories;


        public FileLoggingFilter(LogSeverity logLevel, string[] logCategories)
        {
            _logLevel = logLevel;
            _logCategories = logCategories;
        }

        public bool ApplyFilter(string channel, Object source, LogSeverity severity, object message, params object[] par)
        {
            return severity >= _logLevel
                   && (_logCategories.Length == 0 || _logCategories.Contains(channel));
        }
    }
}
