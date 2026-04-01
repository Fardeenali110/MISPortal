using System;

namespace MisProject.Helpers
{
    public static class LoggerHelper
    {
        public static void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[INFO] {DateTime.Now}: {message}");
        }

        public static void LogError(string message, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            if (ex != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
            }
        }

        public static void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {DateTime.Now}: {message}");
        }

        public static void LogAudit(string action, string details)
        {
            System.Diagnostics.Debug.WriteLine($"[AUDIT] {DateTime.Now}: {action} - {details}");
        }
    }
}