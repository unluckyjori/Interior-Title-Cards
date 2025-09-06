using BepInEx.Logging;

namespace InteriorTitleCards.Core
{
    /// <summary>
    /// Centralized logging system for the Interior Title Cards mod.
    /// </summary>
    public static class Logger
    {
        private static ManualLogSource _logSource;

        /// <summary>
        /// Initializes the logger with the provided log source.
        /// </summary>
        /// <param name="logSource">The BepInEx log source to use for logging.</param>
        public static void Initialize(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message)
        {
            if (_logSource != null)
            {
                _logSource.LogInfo($"[InteriorTitleCards] {message}");
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message)
        {
            if (_logSource != null)
            {
                _logSource.LogWarning($"[InteriorTitleCards] {message}");
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message)
        {
            if (_logSource != null)
            {
                _logSource.LogError($"[InteriorTitleCards] {message}");
            }
        }

        /// <summary>
        /// Logs a debug message if debug logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogDebug(string message)
        {
            if (_logSource != null)
            {
                _logSource.LogInfo($"[InteriorTitleCards DEBUG] {message}");
            }
        }
    }
}