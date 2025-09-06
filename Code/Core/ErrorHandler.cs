using System;
using InteriorTitleCards.Core;

namespace InteriorTitleCards.Core
{
    /// <summary>
    /// Centralized error handling system for the Interior Title Cards mod.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handles an exception by logging it and optionally showing a user-friendly message.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        /// <param name="showToUser">Whether to show the error to the user.</param>
        public static void HandleException(Exception ex, string context = null, bool showToUser = false)
        {
            string message = $"Exception in {context ?? "unknown context"}: {ex.Message}";
            Logger.LogError(message);
            Logger.LogDebug($"Stack trace: {ex.StackTrace}");

            if (showToUser)
            {
                // TODO: Implement user notification system
                Logger.LogWarning("User notification not yet implemented");
            }
        }

        /// <summary>
        /// Safely executes an action with error handling.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="context">Context for error reporting.</param>
        /// <returns>True if the action executed successfully, false otherwise.</returns>
        public static bool SafeExecute(Action action, string context = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, context ?? "SafeExecute");
                return false;
            }
        }

        /// <summary>
        /// Safely executes a function with error handling.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <param name="context">Context for error reporting.</param>
        /// <param name="defaultValue">Default value to return on error.</param>
        /// <returns>The result of the function or the default value on error.</returns>
        public static T SafeExecute<T>(Func<T> func, string context = null, T defaultValue = default)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, context ?? "SafeExecute");
                return defaultValue;
            }
        }
    }
}